﻿using System.Security.Cryptography;
using Phantom.Common.Logging;
using Phantom.Common.Minecraft;
using Phantom.Utils.Cryptography;
using Phantom.Utils.IO;
using Phantom.Utils.Runtime;
using Serilog;

namespace Phantom.Agent.Minecraft.Server;

sealed class MinecraftServerExecutableDownloader {
	private static readonly ILogger Logger = PhantomLogger.Create<MinecraftServerExecutableDownloader>();

	private readonly MinecraftVersions minecraftVersions;
	
	public Task<string?> Task { get; }
	public event EventHandler<DownloadProgressEventArgs>? DownloadProgress;
	public event EventHandler? Completed;
	
	private readonly CancellationTokenSource cancellationTokenSource = new ();
	private int listeners = 0;

	public MinecraftServerExecutableDownloader(MinecraftVersions minecraftVersions, string version, string filePath, MinecraftServerExecutableDownloadListener listener) {
		this.minecraftVersions = minecraftVersions;
		
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

		string tmpFilePath = filePath + ".tmp";

		var cancellationToken = cancellationTokenSource.Token;
		try {
			var serverExecutableInfo = await minecraftVersions.GetServerExecutableInfo(version, cancellationToken);
			if (serverExecutableInfo == null) {
				return null;
			}

			Logger.Information("Downloading server executable from: {Url} ({Size})", serverExecutableInfo.DownloadUrl, serverExecutableInfo.Size.ToHumanReadable(decimalPlaces: 1));
			try {
				using var http = new HttpClient();
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

	private static async Task FetchServerExecutableFile(HttpClient http, DownloadProgressCallback progressCallback, MinecraftServerExecutableInfo info, string filePath, CancellationToken cancellationToken) {
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
