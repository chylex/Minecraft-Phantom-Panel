using System.Security.Cryptography;
using Phantom.Common.Data.Agent;
using Phantom.Utils.Cryptography;
using Phantom.Utils.IO;
using Phantom.Utils.Logging;
using Phantom.Utils.Net;
using Phantom.Utils.Runtime;
using Serilog;

namespace Phantom.Agent.Services.Downloads;

sealed class FileDownloader {
	private static readonly ILogger Logger = PhantomLogger.Create<FileDownloader>();
	
	public Task<string?> Task { get; }
	public event EventHandler<DownloadProgressEventArgs>? DownloadProgress;
	public event EventHandler? Completed;
	
	private readonly CancellationTokenSource cancellationTokenSource = new ();
	
	private readonly List<CancellationTokenRegistration> listenerCancellationRegistrations = [];
	private int listenerCount = 0;
	
	public FileDownloader(FileDownloadInfo fileDownloadInfo, string filePath, FileDownloadListener listener) {
		Register(listener);
		Task = DownloadFileAndGetFinalPath(fileDownloadInfo, filePath, new DownloadProgressCallback(this), cancellationTokenSource.Token);
		Task.ContinueWith(OnCompleted, TaskScheduler.Default);
	}
	
	public void Register(FileDownloadListener listener) {
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
			FileDownloadListener listener = (FileDownloadListener) listenerObject!;
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
	
	private sealed class DownloadProgressCallback(FileDownloader downloader) {
		public void ReportProgress(ulong downloadedBytes, ulong? totalBytes) {
			downloader.ReportDownloadProgress(new DownloadProgressEventArgs(downloadedBytes, totalBytes));
		}
	}
	
	private static async Task<string?> DownloadFileAndGetFinalPath(FileDownloadInfo fileDownloadInfo, string filePath, DownloadProgressCallback progressCallback, CancellationToken cancellationToken) {
		string tmpFilePath = filePath + ".tmp";
		
		try {
			await DownloadFile(tmpFilePath, fileDownloadInfo, progressCallback, cancellationToken);
			MoveDownloadedFile(filePath, tmpFilePath);
		} catch (Exception) {
			TryDeletePartiallyDownloadedFile(tmpFilePath);
			throw;
		}
		
		return filePath;
	}
	
	private static async Task DownloadFile(string filePath, FileDownloadInfo fileDownloadInfo, DownloadProgressCallback progressCallback, CancellationToken cancellationToken) {
		string downloadUrl = fileDownloadInfo.Url;
		
		DownloadResult result;
		try {
			var httpClient = new HttpClient();
			var response = await httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
			response.EnsureSuccessStatusCode();
			
			FileSize? fileSize = response.Headers.ContentLength;
			ulong? fileSizeBytes = fileSize?.Bytes;
			
			Logger.Information("Downloading {Url} ({Size})...", downloadUrl, FormatFileSize(fileSize));
			progressCallback.ReportProgress(downloadedBytes: 0, fileSizeBytes);
			
			await using var fileStream = new FileStream(filePath, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
			await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
			
			using var streamCopier = new DownloadStreamCopier(progressCallback, fileSizeBytes);
			result = await streamCopier.Copy(responseStream, fileStream, cancellationToken);
		} catch (OperationCanceledException) {
			Logger.Information("File download was cancelled: {Url}", downloadUrl);
			throw;
		} catch (Exception e) {
			Logger.Error(e, "Could not download file: {Url}", downloadUrl);
			throw StopProcedureException.Instance;
		}
		
		if (fileDownloadInfo.Hash is {} expectedHash && !result.Hash.Equals(expectedHash)) {
			Logger.Error("Downloaded file from {Url} has mismatched SHA1 hash. Expected {Expected}, got {Actual}.", downloadUrl, expectedHash, result.Hash);
			throw StopProcedureException.Instance;
		}
		
		Logger.Information("Finished downloading {Url} ({Size}).", downloadUrl, FormatFileSize(result.Size));
	}
	
	private static string FormatFileSize(FileSize? fileSize) {
		return fileSize?.ToHumanReadable(decimalPlaces: 1) ?? "unknown size";
	}
	
	private static void MoveDownloadedFile(string filePath, string tmpFilePath) {
		try {
			File.Move(tmpFilePath, filePath, overwrite: true);
		} catch (Exception e) {
			Logger.Error(e, "Could not move downloaded file from {SourcePath} to {TargetPath}", tmpFilePath, filePath);
			throw StopProcedureException.Instance;
		}
	}
	
	private static void TryDeletePartiallyDownloadedFile(string filePath) {
		if (!File.Exists(filePath)) {
			return;
		}
		
		try {
			File.Delete(filePath);
		} catch (Exception e) {
			Logger.Warning(e, "Could not clean up partially downloaded file: {FilePath}", filePath);
		}
	}
	
	private sealed class DownloadStreamCopier : IDisposable {
		private readonly StreamCopier streamCopier = new ();
		private readonly IncrementalHash sha1 = IncrementalHash.CreateHash(HashAlgorithmName.SHA1);
		
		private readonly DownloadProgressCallback progressCallback;
		private readonly ulong? totalBytes;
		private ulong readBytes;
		
		public DownloadStreamCopier(DownloadProgressCallback progressCallback, ulong? totalBytes) {
			this.progressCallback = progressCallback;
			this.totalBytes = totalBytes;
			this.streamCopier.BufferReady += OnBufferReady;
		}
		
		private void OnBufferReady(object? sender, StreamCopier.BufferEventArgs args) {
			sha1.AppendData(args.Buffer.Span);
			
			readBytes += (uint) args.Buffer.Length;
			progressCallback.ReportProgress(readBytes, totalBytes);
		}
		
		public async Task<DownloadResult> Copy(Stream source, Stream destination, CancellationToken cancellationToken) {
			await streamCopier.Copy(source, destination, cancellationToken);
			
			FileSize size = new FileSize(readBytes);
			Sha1String hash = Sha1String.FromBytes(sha1.GetHashAndReset());
			return new DownloadResult(size, hash);
		}
		
		public void Dispose() {
			sha1.Dispose();
			streamCopier.Dispose();
		}
	}
	
	private readonly record struct DownloadResult(FileSize Size, Sha1String Hash);
}
