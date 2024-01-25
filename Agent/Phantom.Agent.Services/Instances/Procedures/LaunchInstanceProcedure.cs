using Phantom.Agent.Minecraft.Instance;
using Phantom.Agent.Minecraft.Launcher;
using Phantom.Agent.Minecraft.Server;
using Phantom.Agent.Services.Instances.States;
using Phantom.Common.Data.Instance;

namespace Phantom.Agent.Services.Instances.Procedures;

sealed record LaunchInstanceProcedure(InstanceConfiguration Configuration, IServerLauncher Launcher, bool IsRestarting = false) : IInstanceProcedure {
	public async Task<IInstanceState?> Run(IInstanceContext context, CancellationToken cancellationToken) {
		if (!IsRestarting && context.CurrentState is InstanceRunningState) {
			return null;
		}
		
		context.SetStatus(IsRestarting ? InstanceStatus.Restarting : InstanceStatus.Launching);

		InstanceLaunchFailReason? failReason = context.Services.PortManager.Reserve(Configuration) switch {
			PortManager.Result.ServerPortNotAllowed   => InstanceLaunchFailReason.ServerPortNotAllowed,
			PortManager.Result.ServerPortAlreadyInUse => InstanceLaunchFailReason.ServerPortAlreadyInUse,
			PortManager.Result.RconPortNotAllowed     => InstanceLaunchFailReason.RconPortNotAllowed,
			PortManager.Result.RconPortAlreadyInUse   => InstanceLaunchFailReason.RconPortAlreadyInUse,
			_                                         => null
		};

		if (failReason is {} reason) {
			context.SetLaunchFailedStatusAndReportEvent(reason);
			return new InstanceNotRunningState();
		}
		
		context.Logger.Information("Session starting...");
		try {
			InstanceProcess process = await DoLaunch(context, cancellationToken);
			return new InstanceRunningState(Configuration, Launcher, process, context);
		} catch (OperationCanceledException) {
			context.SetStatus(InstanceStatus.NotRunning);
		} catch (LaunchFailureException e) {
			context.Logger.Error(e.LogMessage);
			context.SetLaunchFailedStatusAndReportEvent(e.Reason);
		} catch (Exception e) {
			context.Logger.Error(e, "Caught exception while launching instance.");
			context.SetLaunchFailedStatusAndReportEvent(InstanceLaunchFailReason.UnknownError);
		}
		
		context.Services.PortManager.Release(Configuration);
		return new InstanceNotRunningState();
	}

	private async Task<InstanceProcess> DoLaunch(IInstanceContext context, CancellationToken cancellationToken) {
		cancellationToken.ThrowIfCancellationRequested();

		byte lastDownloadProgress = byte.MaxValue;

		void OnDownloadProgress(object? sender, DownloadProgressEventArgs args) {
			byte progress = (byte) Math.Min(args.DownloadedBytes * 100 / args.TotalBytes, 100);

			if (lastDownloadProgress != progress) {
				lastDownloadProgress = progress;
				context.SetStatus(InstanceStatus.Downloading(progress));
			}
		}

		var launchResult = await Launcher.Launch(context.Logger, context.Services.LaunchServices, OnDownloadProgress, cancellationToken);
		if (launchResult is LaunchResult.InvalidJavaRuntime) {
			throw new LaunchFailureException(InstanceLaunchFailReason.JavaRuntimeNotFound, "Session failed to launch, invalid Java runtime.");
		}
		else if (launchResult is LaunchResult.CouldNotDownloadMinecraftServer) {
			throw new LaunchFailureException(InstanceLaunchFailReason.CouldNotDownloadMinecraftServer, "Session failed to launch, could not download Minecraft server.");
		}
		else if (launchResult is LaunchResult.CouldNotPrepareMinecraftServerLauncher) {
			throw new LaunchFailureException(InstanceLaunchFailReason.CouldNotPrepareMinecraftServerLauncher, "Session failed to launch, could not prepare Minecraft server launcher.");
		}
		else if (launchResult is LaunchResult.CouldNotConfigureMinecraftServer) {
			throw new LaunchFailureException(InstanceLaunchFailReason.CouldNotConfigureMinecraftServer, "Session failed to launch, could not configure Minecraft server.");
		}
		else if (launchResult is LaunchResult.CouldNotStartMinecraftServer) {
			throw new LaunchFailureException(InstanceLaunchFailReason.CouldNotStartMinecraftServer, "Session failed to launch, could not start Minecraft server.");
		}

		if (launchResult is not LaunchResult.Success launchSuccess) {
			throw new LaunchFailureException(InstanceLaunchFailReason.UnknownError, "Session failed to launch.");
		}

		context.SetStatus(InstanceStatus.Running);
		context.ReportEvent(InstanceEvent.LaunchSucceeded);
		return launchSuccess.Process;
	}

	private sealed class LaunchFailureException : Exception {
		public InstanceLaunchFailReason Reason { get; }
		public string LogMessage { get; }

		public LaunchFailureException(InstanceLaunchFailReason reason, string logMessage) {
			this.Reason = reason;
			this.LogMessage = logMessage;
		}
	}
}
