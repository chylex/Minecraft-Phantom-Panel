using Phantom.Agent.Minecraft.Instance;
using Phantom.Agent.Minecraft.Launcher;
using Phantom.Agent.Minecraft.Server;
using Phantom.Agent.Services.Instances.States;
using Phantom.Common.Data.Instance;
using Phantom.Utils.Tasks;

namespace Phantom.Agent.Services.Instances.Procedures;

static class InstanceLaunchProcedure {
	public static async Task<IInstanceState> Run(InstanceContext context, InstanceConfiguration configuration, IServerLauncher launcher, InstanceTicketManager.Ticket ticket, Action<IInstanceStatus> reportStatus, CancellationToken cancellationToken) {
		context.Logger.Information("Session starting...");

		var newState = await TryLaunchInstance(context, configuration, launcher, ticket, reportStatus, cancellationToken);
		if (newState is not InstanceRunningState) {
			context.Services.InstanceTicketManager.Release(ticket);
		}
		
		return newState;
	}

	private static async Task<IInstanceState> TryLaunchInstance(InstanceContext context, InstanceConfiguration configuration, IServerLauncher launcher, InstanceTicketManager.Ticket ticket, Action<IInstanceStatus> reportStatus, CancellationToken cancellationToken) {
		Result<InstanceProcess, InstanceLaunchFailReason> result;
		
		try {
			result = await LaunchInstanceImpl(context, launcher, reportStatus, cancellationToken);
		} catch (OperationCanceledException) {
			reportStatus(InstanceStatus.NotRunning);
			return new InstanceNotRunningState();
		} catch (Exception e) {
			context.Logger.Error(e, "Caught exception while launching instance.");
			result = InstanceLaunchFailReason.UnknownError;
		}

		if (result) {
			reportStatus(InstanceStatus.Running);
			context.ReportEvent(InstanceEvent.LaunchSucceeded);
			return new InstanceRunningState(context, configuration, launcher, ticket, result.Value, cancellationToken);
		}
		else {
			reportStatus(InstanceStatus.Failed(result.Error));
			context.ReportEvent(new InstanceLaunchFailedEvent(result.Error));
			return new InstanceNotRunningState();
		}
	}

	private static async Task<Result<InstanceProcess, InstanceLaunchFailReason>> LaunchInstanceImpl(InstanceContext context, IServerLauncher launcher, Action<IInstanceStatus> reportStatus, CancellationToken cancellationToken) {
		cancellationToken.ThrowIfCancellationRequested();

		byte lastDownloadProgress = byte.MaxValue;

		void OnDownloadProgress(object? sender, DownloadProgressEventArgs args) {
			byte progress = (byte) Math.Min(args.DownloadedBytes * 100 / args.TotalBytes, 100);

			if (lastDownloadProgress != progress) {
				lastDownloadProgress = progress;
				reportStatus(InstanceStatus.Downloading(progress));
			}
		}

		switch (await launcher.Launch(context.Logger, context.Services.LaunchServices, OnDownloadProgress, cancellationToken)) {
			case LaunchResult.Success launchSuccess:
				return launchSuccess.Process;

			case LaunchResult.InvalidJavaRuntime:
				context.Logger.Error("Session failed to launch, invalid Java runtime.");
				return InstanceLaunchFailReason.JavaRuntimeNotFound;

			case LaunchResult.CouldNotDownloadMinecraftServer:
				context.Logger.Error("Session failed to launch, could not download Minecraft server.");
				return InstanceLaunchFailReason.CouldNotDownloadMinecraftServer;

			case LaunchResult.CouldNotPrepareMinecraftServerLauncher:
				context.Logger.Error("Session failed to launch, could not prepare Minecraft server launcher.");
				return InstanceLaunchFailReason.CouldNotPrepareMinecraftServerLauncher;

			case LaunchResult.CouldNotConfigureMinecraftServer:
				context.Logger.Error("Session failed to launch, could not configure Minecraft server.");
				return InstanceLaunchFailReason.CouldNotConfigureMinecraftServer;

			case LaunchResult.CouldNotStartMinecraftServer:
				context.Logger.Error("Session failed to launch, could not start Minecraft server.");
				return InstanceLaunchFailReason.CouldNotStartMinecraftServer;

			default:
				context.Logger.Error("Session failed to launch.");
				return InstanceLaunchFailReason.UnknownError;
		}
	}
}
