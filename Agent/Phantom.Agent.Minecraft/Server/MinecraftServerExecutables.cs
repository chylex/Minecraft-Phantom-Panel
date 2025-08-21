using System.Text.RegularExpressions;
using Phantom.Common.Data.Minecraft;
using Phantom.Utils.IO;
using Phantom.Utils.Logging;
using Serilog;

namespace Phantom.Agent.Minecraft.Server;

public sealed partial class MinecraftServerExecutables {
	private static readonly ILogger Logger = PhantomLogger.Create<MinecraftServerExecutables>();
	
	[GeneratedRegex(@"[^a-zA-Z0-9_\-\.]", RegexOptions.Compiled)]
	private static partial Regex SanitizePathRegex();
	
	private readonly string basePath;
	private readonly Dictionary<string, MinecraftServerExecutableDownloader> runningDownloadersByVersion = new ();
	
	public MinecraftServerExecutables(string basePath) {
		this.basePath = basePath;
	}
	
	internal async Task<string?> DownloadAndGetPath(FileDownloadInfo? fileDownloadInfo, string minecraftVersion, EventHandler<DownloadProgressEventArgs> progressEventHandler, CancellationToken cancellationToken) {
		string serverExecutableFolderPath = Path.Combine(basePath, SanitizePathRegex().IsMatch(minecraftVersion) ? SanitizePathRegex().Replace(minecraftVersion, "_") : minecraftVersion);
		string serverExecutableFilePath = Path.Combine(serverExecutableFolderPath, "server.jar");
		
		if (File.Exists(serverExecutableFilePath)) {
			return serverExecutableFilePath;
		}
		
		if (fileDownloadInfo == null) {
			Logger.Error("Unable to download server executable for version {Version} because no download info was provided.", minecraftVersion);
			return null;
		}
		
		try {
			Directories.Create(serverExecutableFolderPath, Chmod.URWX_GRX);
		} catch (Exception e) {
			Logger.Error(e, "Unable to create folder for server executable: {ServerExecutableFolderPath}", serverExecutableFolderPath);
			return null;
		}
		
		MinecraftServerExecutableDownloader? downloader;
		MinecraftServerExecutableDownloadListener listener = new (progressEventHandler, cancellationToken);
		
		lock (this) {
			if (runningDownloadersByVersion.TryGetValue(minecraftVersion, out downloader)) {
				Logger.Information("A download for server version {Version} is already running, waiting for it to finish...", minecraftVersion);
				downloader.Register(listener);
			}
			else {
				downloader = new MinecraftServerExecutableDownloader(fileDownloadInfo, minecraftVersion, serverExecutableFilePath, listener);
				downloader.Completed += (_, _) => {
					lock (this) {
						runningDownloadersByVersion.Remove(minecraftVersion);
					}
				};
				
				runningDownloadersByVersion[minecraftVersion] = downloader;
			}
		}
		
		return await downloader.Task.WaitAsync(cancellationToken);
	}
}
