using System.Text.RegularExpressions;
using Phantom.Common.Logging;
using Phantom.Common.Minecraft;
using Phantom.Utils.IO;
using Serilog;

namespace Phantom.Agent.Minecraft.Server;

public sealed partial class MinecraftServerExecutables : IDisposable {
	private static readonly ILogger Logger = PhantomLogger.Create<MinecraftServerExecutables>();

	[GeneratedRegex(@"[^a-zA-Z0-9_\-\.]", RegexOptions.Compiled)]
	private static partial Regex VersionFolderSanitizeRegex();

	private readonly string basePath;
	private readonly MinecraftVersions minecraftVersions = new ();
	private readonly Dictionary<string, MinecraftServerExecutableDownloader> runningDownloadersByVersion = new ();

	public MinecraftServerExecutables(string basePath) {
		this.basePath = basePath;
	}

	internal async Task<string?> DownloadAndGetPath(string version, EventHandler<DownloadProgressEventArgs> progressEventHandler, CancellationToken cancellationToken) {
		string serverExecutableFolderPath = Path.Combine(basePath, VersionFolderSanitizeRegex().Replace(version, "_"));
		string serverExecutableFilePath = Path.Combine(serverExecutableFolderPath, "server.jar");

		if (File.Exists(serverExecutableFilePath)) {
			return serverExecutableFilePath;
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
			if (runningDownloadersByVersion.TryGetValue(version, out downloader)) {
				Logger.Information("A download for server version {Version} is already running, waiting for it to finish...", version);
				downloader.Register(listener);
			}
			else {
				downloader = new MinecraftServerExecutableDownloader(minecraftVersions, version, serverExecutableFilePath, listener);
				downloader.Completed += (_, _) => {
					lock (this) {
						runningDownloadersByVersion.Remove(version);
					}
				};

				runningDownloadersByVersion[version] = downloader;
			}
		}

		return await downloader.Task.WaitAsync(cancellationToken);
	}

	public void Dispose() {
		minecraftVersions.Dispose();
	}
}
