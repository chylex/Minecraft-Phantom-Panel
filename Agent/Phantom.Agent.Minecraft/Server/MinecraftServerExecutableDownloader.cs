using System.Security.Cryptography;
using Phantom.Common.Data.Minecraft;
using Phantom.Utils.Cryptography;
using Phantom.Utils.IO;
using Phantom.Utils.Logging;
using Phantom.Utils.Runtime;
using Serilog;

namespace Phantom.Agent.Minecraft.Server;

sealed class MinecraftServerExecutableDownloader {
	private static readonly ILogger Logger = PhantomLogger.Create<MinecraftServerExecutableDownloader>();
	
	public Task<string?> Task { get; }
	public event EventHandler<DownloadProgressEventArgs>? DownloadProgress;
	public event EventHandler? Completed;
	
	private readonly CancellationTokenSource cancellationTokenSource = new ();
	
	private readonly List<CancellationTokenRegistration> listenerCancellationRegistrations = new ();
	private int listenerCount = 0;
	
	public MinecraftServerExecutableDownloader(FileDownloadInfo fileDownloadInfo, string minecraftVersion, string filePath, MinecraftServerExecutableDownloadListener listener) {
		Register(listener);
		Task = DownloadAndGetPath(fileDownloadInfo, minecraftVersion, filePath, new DownloadProgressCallback(this), cancellationTokenSource.Token);
		Task.ContinueWith(OnCompleted, TaskScheduler.Default);
	}
	
	public void Register(MinecraftServerExecutableDownloadListener listener) {
		int newListenerCount;
		
		lock (this) {
			newListenerCount = ++listenerCount;
			
			DownloadProgress += listener.DownloadProgressEventHandler;
			listenerCancellationRegistrations.Add(listener.CancellationToken.Register(Unregister, listener));
		}
		
		Logger.Debug("Registered download listener, current listener count: {Listeners}", newListenerCount);
	}
	
	private void Unregister(object? listenerObject) {
		int newListenerCount;
		
		lock (this) {
			MinecraftServerExecutableDownloadListener listener = (MinecraftServerExecutableDownloadListener) listenerObject!;
			DownloadProgress -= listener.DownloadProgressEventHandler;
			
			newListenerCount = --listenerCount;
			if (newListenerCount <= 0) {
				cancellationTokenSource.Cancel();
			}
		}
		
		if (newListenerCount <= 0) {
			Logger.Debug("Unregistered last download listener, cancelling download.");
		}
		else {
			Logger.Debug("Unregistered download listener, current listener count: {Listeners}", newListenerCount);
		}
	}
	
	private void ReportDownloadProgress(DownloadProgressEventArgs args) {
		DownloadProgress?.Invoke(this, args);
	}
	
	private void OnCompleted(Task task) {
		Logger.Debug("Download task completed.");
		
		lock (this) {
			Completed?.Invoke(this, EventArgs.Empty);
			Completed = null;
			DownloadProgress = null;
			
			foreach (var registration in listenerCancellationRegistrations) {
				registration.Dispose();
			}
			
			listenerCancellationRegistrations.Clear();
			cancellationTokenSource.Dispose();
		}
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
	
	private static async Task<string?> DownloadAndGetPath(FileDownloadInfo fileDownloadInfo, string minecraftVersion, string filePath, DownloadProgressCallback progressCallback, CancellationToken cancellationToken) {
		string tmpFilePath = filePath + ".tmp";
		
		try {
			Logger.Information("Downloading server version {Version} from: {Url} ({Size})", minecraftVersion, fileDownloadInfo.DownloadUrl, fileDownloadInfo.Size.ToHumanReadable(decimalPlaces: 1));
			try {
				using var http = new HttpClient();
				await FetchServerExecutableFile(http, progressCallback, fileDownloadInfo, tmpFilePath, cancellationToken);
			} catch (Exception) {
				TryDeleteExecutableAfterFailure(tmpFilePath);
				throw;
			}
			
			File.Move(tmpFilePath, filePath, true);
			Logger.Information("Server version {Version} downloaded.", minecraftVersion);
			
			return filePath;
		} catch (OperationCanceledException) {
			Logger.Information("Download for server version {Version} was cancelled.", minecraftVersion);
			throw;
		} catch (StopProcedureException) {
			return null;
		} catch (Exception e) {
			Logger.Error(e, "An unexpected error occurred.");
			return null;
		}
	}
	
	private static async Task FetchServerExecutableFile(HttpClient http, DownloadProgressCallback progressCallback, FileDownloadInfo fileDownloadInfo, string filePath, CancellationToken cancellationToken) {
		Sha1String downloadedFileHash;
		
		try {
			var response = await http.GetAsync(fileDownloadInfo.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
			response.EnsureSuccessStatusCode();
			
			await using var fileStream = new FileStream(filePath, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
			await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
			
			using var streamCopier = new MinecraftServerDownloadStreamCopier(progressCallback, fileDownloadInfo.Size.Bytes);
			downloadedFileHash = await streamCopier.Copy(responseStream, fileStream, cancellationToken);
		} catch (OperationCanceledException) {
			throw;
		} catch (Exception e) {
			Logger.Error(e, "Unable to download server executable.");
			throw StopProcedureException.Instance;
		}
		
		if (!downloadedFileHash.Equals(fileDownloadInfo.Hash)) {
			Logger.Error("Downloaded server executable has mismatched SHA1 hash. Expected {Expected}, got {Actual}.", fileDownloadInfo.Hash, downloadedFileHash);
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
}
