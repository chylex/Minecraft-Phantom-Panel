using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using Phantom.Common.Logging;
using Phantom.Utils.Cryptography;
using Phantom.Utils.IO;
using Serilog;

namespace Phantom.Agent.Minecraft.Server;

sealed class MinecraftServerExecutableDownloader {
	private static readonly ILogger Logger = PhantomLogger.Create<MinecraftServerExecutableDownloader>();

	private const string VersionManifestUrl = "https://launchermeta.mojang.com/mc/game/version_manifest.json";

	public Task<string?> Task { get; }
	public event EventHandler<DownloadProgressEventArgs>? DownloadProgress;
	public event EventHandler? Completed;
	
	private readonly CancellationTokenSource cancellationTokenSource = new ();
	private int listeners = 0;

	public MinecraftServerExecutableDownloader(string version, string filePath, MinecraftServerExecutableDownloadListener listener) {
		Register(listener);
		Task = DownloadAndGetPath(version, filePath);
		Task.ContinueWith(OnCompleted, TaskScheduler.Default);
	}

	public void Register(MinecraftServerExecutableDownloadListener listener) {
		++listeners;
		Logger.Verbose("Registered download listener, current listener count: {Listeners}", listeners);
		
		DownloadProgress += listener.DownloadProgressEventHandler;
		listener.CancellationToken.Register(Unregister, listener);
	}

	private void Unregister(object? listenerObject) {
		MinecraftServerExecutableDownloadListener listener = (MinecraftServerExecutableDownloadListener) listenerObject!;
		DownloadProgress -= listener.DownloadProgressEventHandler;

		if (--listeners <= 0) {
			Logger.Verbose("Unregistered last download listener, cancelling download.");
			cancellationTokenSource.Cancel();
		}
		else {
			Logger.Verbose("Unregistered download listener, current listener count: {Listeners}", listeners);
		}
	}

	private void ReportDownloadProgress(DownloadProgressEventArgs args) {
		DownloadProgress?.Invoke(this, args);
	}

	private void OnCompleted(Task task) {
		Logger.Verbose("Download task completed.");
		Completed?.Invoke(this, EventArgs.Empty);
		Completed = null;
		DownloadProgress = null;
	}

	private sealed class DownloadProgressCallback {
		private readonly MinecraftServerExecutableDownloader downloader;

		public DownloadProgressCallback(MinecraftServerExecutableDownloader downloader) {
			this.downloader = downloader;
		}

		public void ReportProgress(ulong downloadedBytes, ulong totalBytes) {
			downloader.ReportDownloadProgress(new DownloadProgressEventArgs(downloadedBytes, totalBytes));
		}
	}

	private async Task<string?> DownloadAndGetPath(string version, string filePath) {
		Logger.Information("Downloading server version {Version}...", version);

		HttpClient http = new HttpClient();
		string tmpFilePath = filePath + ".tmp";

		var cancellationToken = cancellationTokenSource.Token;
		try {
			Logger.Information("Fetching version manifest from: {Url}", VersionManifestUrl);
			var versionManifest = await FetchVersionManifest(http, cancellationToken);
			var metadataUrl = GetVersionMetadataUrlFromManifest(version, versionManifest);

			Logger.Information("Fetching metadata for version {Version} from: {Url}", version, metadataUrl);
			var versionMetadata = await FetchVersionMetadata(http, metadataUrl, cancellationToken);
			var serverExecutableInfo = GetServerExecutableUrlFromMetadata(versionMetadata);

			Logger.Information("Downloading server executable from: {Url} ({Size})", serverExecutableInfo.DownloadUrl, serverExecutableInfo.Size.ToHumanReadable(decimalPlaces: 1));
			try {
				await FetchServerExecutableFile(http, new DownloadProgressCallback(this), serverExecutableInfo, tmpFilePath, cancellationToken);
			} catch (Exception) {
				TryDeleteExecutableAfterFailure(tmpFilePath);
				throw;
			}

			File.Move(tmpFilePath, filePath, true);
			Logger.Information("Server version {Version} downloaded.", version);

			return filePath;
		} catch (OperationCanceledException) {
			Logger.Information("Download for server version {Version} was cancelled.", version);
			throw;
		} catch (StopProcedureException) {
			return null;
		} catch (Exception e) {
			Logger.Error(e, "An unexpected error occurred.");
			return null;
		} finally {
			cancellationTokenSource.Dispose();
		}
	}

	private static async Task<JsonElement> FetchVersionManifest(HttpClient http, CancellationToken cancellationToken) {
		try {
			return await http.GetFromJsonAsync<JsonElement>(VersionManifestUrl, cancellationToken);
		} catch (HttpRequestException e) {
			Logger.Error(e, "Unable to download version manifest.");
			throw StopProcedureException.Instance;
		} catch (Exception e) {
			Logger.Error(e, "Unable to parse version manifest as JSON.");
			throw StopProcedureException.Instance;
		}
	}

	private static async Task<JsonElement> FetchVersionMetadata(HttpClient http, string metadataUrl, CancellationToken cancellationToken) {
		try {
			return await http.GetFromJsonAsync<JsonElement>(metadataUrl, cancellationToken);
		} catch (HttpRequestException e) {
			Logger.Error(e, "Unable to download version metadata.");
			throw StopProcedureException.Instance;
		} catch (Exception e) {
			Logger.Error(e, "Unable to parse version metadata as JSON.");
			throw StopProcedureException.Instance;
		}
	}

	private static async Task FetchServerExecutableFile(HttpClient http, DownloadProgressCallback progressCallback, ServerExecutableInfo info, string filePath, CancellationToken cancellationToken) {
		Sha1String downloadedFileHash;

		try {
			var response = await http.GetAsync(info.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
			response.EnsureSuccessStatusCode();

			await using var fileStream = new FileStream(filePath, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
			await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);

			using var streamCopier = new MinecraftServerDownloadStreamCopier(progressCallback, info.Size.Bytes);
			downloadedFileHash = await streamCopier.Copy(responseStream, fileStream, cancellationToken);
		} catch (OperationCanceledException) {
			throw;
		} catch (Exception e) {
			Logger.Error(e, "Unable to download server executable.");
			throw StopProcedureException.Instance;
		}

		if (!downloadedFileHash.Equals(info.Hash)) {
			Logger.Error("Downloaded server executable has mismatched SHA1 hash. Expected {Expected}, got {Actual}.", info.Hash, downloadedFileHash);
			throw StopProcedureException.Instance;
		}
	}

	private static void TryDeleteExecutableAfterFailure(string filePath) {
		if (File.Exists(filePath)) {
			try {
				File.Delete(filePath);
			} catch (Exception e) {
				Logger.Warning(e, "Could not clean up partially downloaded server executable: {FilePath}", filePath);
			}
		}
	}

	private static string GetVersionMetadataUrlFromManifest(string serverVersion, JsonElement versionManifest) {
		JsonElement versionsElement = GetJsonPropertyOrThrow(versionManifest, "versions", JsonValueKind.Array, "version manifest");
		JsonElement versionElement;
		try {
			versionElement = versionsElement.EnumerateArray().Single(ele => ele.TryGetProperty("id", out var id) && id.ValueKind == JsonValueKind.String && id.GetString() == serverVersion);
		} catch (Exception) {
			Logger.Error("Version {Version} was not found in version manifest.", serverVersion);
			throw StopProcedureException.Instance;
		}

		JsonElement urlElement = GetJsonPropertyOrThrow(versionElement, "url", JsonValueKind.String, "version entry in version manifest");
		string? url = urlElement.GetString();

		if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) {
			Logger.Error("The \"url\" key in version entry in version manifest does not contain a valid URL: {Url}", url);
			throw StopProcedureException.Instance;
		}

		if (uri.Scheme != "https" || !uri.AbsolutePath.EndsWith(".json", StringComparison.OrdinalIgnoreCase)) {
			Logger.Error("The \"url\" key in version entry in version manifest does not contain a accepted URL: {Url}", url);
			throw StopProcedureException.Instance;
		}

		return url;
	}

	private static ServerExecutableInfo GetServerExecutableUrlFromMetadata(JsonElement versionMetadata) {
		JsonElement downloadsElement = GetJsonPropertyOrThrow(versionMetadata, "downloads", JsonValueKind.Object, "version metadata");
		JsonElement serverElement = GetJsonPropertyOrThrow(downloadsElement, "server", JsonValueKind.Object, "downloads object in version metadata");
		JsonElement urlElement = GetJsonPropertyOrThrow(serverElement, "url", JsonValueKind.String, "downloads.server object in version metadata");
		string? url = urlElement.GetString();

		if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) {
			Logger.Error("The \"url\" key in downloads.server object in version metadata does not contain a valid URL: {Url}", url);
			throw StopProcedureException.Instance;
		}

		if (uri.Scheme != "https" || !uri.AbsolutePath.EndsWith(".jar", StringComparison.OrdinalIgnoreCase)) {
			Logger.Error("The \"url\" key in downloads.server object in version metadata does not contain a accepted URL: {Url}", url);
			throw StopProcedureException.Instance;
		}

		JsonElement sizeElement = GetJsonPropertyOrThrow(serverElement, "size", JsonValueKind.Number, "downloads.server object in version metadata");
		ulong size;
		try {
			size = sizeElement.GetUInt64();
		} catch (FormatException) {
			Logger.Error("The \"size\" key in downloads.server object in version metadata contains an invalid file size: {Size}", sizeElement);
			throw StopProcedureException.Instance;
		}

		JsonElement sha1Element = GetJsonPropertyOrThrow(serverElement, "sha1", JsonValueKind.String, "downloads.server object in version metadata");
		Sha1String hash;
		try {
			hash = Sha1String.FromString(sha1Element.GetString());
		} catch (Exception) {
			Logger.Error("The \"sha1\" key in downloads.server object in version metadata does not contain a valid SHA-1 hash: {Sha1}", sha1Element.GetString());
			throw StopProcedureException.Instance;
		}

		return new ServerExecutableInfo(url, hash, new FileSize(size));
	}

	private static JsonElement GetJsonPropertyOrThrow(JsonElement parentElement, string propertyKey, JsonValueKind expectedKind, string location) {
		if (!parentElement.TryGetProperty(propertyKey, out var valueElement)) {
			Logger.Error("Missing \"{Property}\" key in " + location + ".", propertyKey);
			throw StopProcedureException.Instance;
		}

		if (valueElement.ValueKind != expectedKind) {
			Logger.Error("The \"{Property}\" key in " + location + " does not contain a JSON {ExpectedType}. Actual type: {ActualType}", propertyKey, expectedKind, valueElement.ValueKind);
			throw StopProcedureException.Instance;
		}

		return valueElement;
	}

	private sealed class MinecraftServerDownloadStreamCopier : IDisposable {
		private readonly StreamCopier streamCopier = new ();
		private readonly IncrementalHash sha1 = IncrementalHash.CreateHash(HashAlgorithmName.SHA1);

		private readonly DownloadProgressCallback progressCallback;
		private readonly ulong totalBytes;
		private ulong readBytes;

		public MinecraftServerDownloadStreamCopier(DownloadProgressCallback progressCallback, ulong totalBytes) {
			this.progressCallback = progressCallback;
			this.totalBytes = totalBytes;
			this.streamCopier.BufferReady += OnBufferReady;
		}

		private void OnBufferReady(object? sender, StreamCopier.BufferEventArgs args) {
			sha1.AppendData(args.Buffer.Span);

			readBytes += (uint) args.Buffer.Length;
			progressCallback.ReportProgress(readBytes, totalBytes);
		}

		public async Task<Sha1String> Copy(Stream source, Stream destination, CancellationToken cancellationToken) {
			await streamCopier.Copy(source, destination, cancellationToken);
			return Sha1String.FromBytes(sha1.GetHashAndReset());
		}

		public void Dispose() {
			sha1.Dispose();
			streamCopier.Dispose();
		}
	}

	private sealed class StopProcedureException : Exception {
		public static StopProcedureException Instance { get; } = new ();

		private StopProcedureException() {}
	}
}
