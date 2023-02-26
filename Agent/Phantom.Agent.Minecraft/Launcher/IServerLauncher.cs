using Phantom.Agent.Minecraft.Server;
using Serilog;

namespace Phantom.Agent.Minecraft.Launcher;

public interface IServerLauncher {
	Task<LaunchResult> Launch(ILogger logger, LaunchServices services, EventHandler<DownloadProgressEventArgs> downloadProgressEventHandler, CancellationToken cancellationToken);
}
