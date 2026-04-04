using Phantom.Common.Data.Agent;
using Phantom.Utils.IO;
using Phantom.Utils.Logging;
using Serilog;

namespace Phantom.Agent.Services.Downloads;

sealed class FileDownloadManager {
	private static readonly ILogger Logger = PhantomLogger.Create<FileDownloadManager>();
	
	private readonly Dictionary<string, FileDownloader> runningDownloadersByPath = new ();
	
	public async Task<string?> DownloadAndGetPath(FileDownloadInfo fileDownloadInfo, string filePath, EventHandler<DownloadProgressEventArgs> progressEventHandler, CancellationToken cancellationToken) {
		var fileInfo = new FileInfo(filePath);
		if (fileInfo.Exists) {
			return filePath;
		}
		
		filePath = fileInfo.FullName;
		
		if (Directory.GetParent(filePath) is {} parentPath) {
			try {
				Directories.Create(parentPath.FullName, Chmod.URWX_GRX);
			} catch (Exception e) {
				Logger.Error(e, "Unable to create directory: {DirectoryName}", parentPath.FullName);
				return null;
			}
		}
		
		FileDownloader? downloader;
		FileDownloadListener listener = new (progressEventHandler, cancellationToken);
		
		lock (this) {
			if (runningDownloadersByPath.TryGetValue(filePath, out downloader)) {
				Logger.Information("A download for {Path} is already running, waiting for it to finish...", filePath);
				downloader.Register(listener);
			}
			else {
				downloader = new FileDownloader(fileDownloadInfo, filePath, listener);
				downloader.Completed += (_, _) => {
					lock (this) {
						runningDownloadersByPath.Remove(filePath);
					}
				};
				
				runningDownloadersByPath[filePath] = downloader;
			}
		}
		
		return await downloader.Task.WaitAsync(cancellationToken);
	}
}
