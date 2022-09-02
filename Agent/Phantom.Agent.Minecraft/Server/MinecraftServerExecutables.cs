using System.Text.RegularExpressions;
using Phantom.Common.Logging;
using Serilog;

namespace Phantom.Agent.Minecraft.Server;

public sealed class MinecraftServerExecutables {
	private static readonly ILogger Logger = PhantomLogger.Create<MinecraftServerExecutables>();

	private static readonly Regex VersionFolderSanitizeRegex = new (@"[^a-zA-Z0-9_\-\.]", RegexOptions.Compiled);

	private readonly string basePath;
	private readonly Dictionary<string, MinecraftServerExecutableDownloader> runningDownloadersByVersion = new ();

	public MinecraftServerExecutables(string basePath) {
		this.basePath = basePath;
	}

	public async Task<string?> DownloadAndGetPath(string version, CancellationToken cancellationToken) {
		string serverExecutableFolderPath = Path.Combine(basePath, VersionFolderSanitizeRegex.Replace(version, "_"));
		string serverExecutableFilePath = Path.Combine(serverExecutableFolderPath, "server.jar");

		if (File.Exists(serverExecutableFilePath)) {
			return serverExecutableFilePath;
		}

		try {
			Directory.CreateDirectory(serverExecutableFolderPath);
		} catch (Exception e) {
			Logger.Error(e, "Unable to create folder for server executable: {ServerExecutableFolderPath}", serverExecutableFolderPath);
			return null;
		}

		MinecraftServerExecutableDownloader downloader;

		lock (this) {
			if (runningDownloadersByVersion.TryGetValue(version, out var existingTask)) {
				Logger.Information("A download for server version {Version} is already running, waiting for it to finish...", version);
				downloader = existingTask;
			}
			else {
				downloader = new MinecraftServerExecutableDownloader(version, serverExecutableFilePath, cancellationToken);
				runningDownloadersByVersion[version] = downloader;
			}
		}

		var result = await downloader.Task;

		lock (this) {
			runningDownloadersByVersion.Remove(version);
		}

		return result;
	}
}
