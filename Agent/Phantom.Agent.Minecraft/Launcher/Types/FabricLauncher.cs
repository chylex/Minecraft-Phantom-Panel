using System.Collections.Immutable;
using Phantom.Agent.Minecraft.Instance;
using Phantom.Utils.IO;
using Serilog;

namespace Phantom.Agent.Minecraft.Launcher.Types;

public sealed class FabricLauncher : BaseLauncher {
	public FabricLauncher(InstanceProperties instanceProperties) : base(instanceProperties) {}
	
	private protected override async Task<ServerJarInfo> PrepareServerJar(ILogger logger, string serverJarPath, CancellationToken cancellationToken) {
		var serverJarParentFolderPath = Directory.GetParent(serverJarPath);
		if (serverJarParentFolderPath == null) {
			throw new ArgumentException("Could not get parent folder from: " + serverJarPath, nameof(serverJarPath));
		}
		
		var launcherJarPath = Path.Combine(serverJarParentFolderPath.FullName, "fabric.jar");
		
		if (!File.Exists(launcherJarPath)) {
			await DownloadLauncher(logger, launcherJarPath, cancellationToken);
		}

		return new ServerJarInfo(launcherJarPath, ImmutableArray.Create("-Dfabric.installer.server.gameJar=" + Paths.NormalizeSlashes(serverJarPath)));
	}

	private async Task DownloadLauncher(ILogger logger, string targetFilePath, CancellationToken cancellationToken) {
		// TODO customizable loader version, probably with a dedicated temporary folder
		string installerUrl = $"https://meta.fabricmc.net/v2/versions/loader/{MinecraftVersion}/stable/stable/server/jar";
		logger.Information("Downloading Fabric launcher from: {Url}", installerUrl);
		
		using var http = new HttpClient();
		
		var response = await http.GetAsync(installerUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
		response.EnsureSuccessStatusCode();
		
		try {
			await using var fileStream = new FileStream(targetFilePath, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
			await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
			await responseStream.CopyToAsync(fileStream, cancellationToken);
		} catch (Exception) {
			TryDeleteLauncherAfterFailure(logger, targetFilePath);
			throw;
		}
	}

	private static void TryDeleteLauncherAfterFailure(ILogger logger, string filePath) {
		if (File.Exists(filePath)) {
			try {
				File.Delete(filePath);
			} catch (Exception e) {
				logger.Warning(e, "Could not clean up partially downloaded Fabric launcher: {FilePath}", filePath);
			}
		}
	}
}
