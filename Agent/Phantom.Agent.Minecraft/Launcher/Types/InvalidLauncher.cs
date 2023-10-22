using Phantom.Agent.Minecraft.Server;
using Serilog;

namespace Phantom.Agent.Minecraft.Launcher.Types;

public sealed class InvalidLauncher : IServerLauncher {
	public static InvalidLauncher Instance { get; } = new ();
	
	private InvalidLauncher() {}
	
	public Task<LaunchResult> Launch(ILogger logger, LaunchServices services, EventHandler<DownloadProgressEventArgs> downloadProgressEventHandler, CancellationToken cancellationToken) {
		return Task.FromResult<LaunchResult>(new LaunchResult.CouldNotPrepareMinecraftServerLauncher());
	}
}
