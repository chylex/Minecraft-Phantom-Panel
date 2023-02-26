using Phantom.Agent.Minecraft.Instance;
using Phantom.Agent.Minecraft.Launcher;
using Phantom.Agent.Minecraft.Server;
using Phantom.Agent.Services.Instances.Sessions;
using Phantom.Common.Data.Instance;
using Phantom.Common.Data.Minecraft;
using Phantom.Common.Data.Replies;

namespace Phantom.Agent.Services.Instances.States;

sealed class InstanceLaunchingState : IInstanceState, IDisposable {
	private readonly InstanceContext context;
	private readonly CancellationTokenSource cancellationTokenSource = new ();
	private byte lastDownloadProgress = byte.MaxValue;

	public InstanceLaunchingState(InstanceContext context) {
		this.context = context;
	}

	public void Initialize() {
		context.Logger.Information("Session starting...");

		var launchTask = context.Services.TaskManager.Run("Launch procedure for instance " + context.ShortName, DoLaunch);
		launchTask.ContinueWith(OnLaunchSuccess, CancellationToken.None, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Default);
		launchTask.ContinueWith(OnLaunchFailure, CancellationToken.None, TaskContinuationOptions.NotOnRanToCompletion, TaskScheduler.Default);
	}

	private async Task<InstanceProcess> DoLaunch() {
		var cancellationToken = cancellationTokenSource.Token;
		cancellationToken.ThrowIfCancellationRequested();

		void OnDownloadProgress(object? sender, DownloadProgressEventArgs args) {
			byte progress = (byte) Math.Min(args.DownloadedBytes * 100 / args.TotalBytes, 100);

			if (lastDownloadProgress != progress) {
				lastDownloadProgress = progress;
				context.SetStatus(InstanceStatus.Downloading(progress));
			}
		}

		var launchResult = await context.Launcher.Launch(context.Logger, context.Services.LaunchServices, OnDownloadProgress, cancellationToken);
		if (launchResult is LaunchResult.InvalidJavaRuntime) {
			throw new LaunchFailureException(InstanceLaunchFailReason.JavaRuntimeNotFound, "Session failed to launch, invalid Java runtime.");
		}
		else if (launchResult is LaunchResult.InvalidJvmArguments) {
			throw new LaunchFailureException(InstanceLaunchFailReason.InvalidJvmArguments, "Session failed to launch, invalid JVM arguments.");
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

		context.SetStatus(InstanceStatus.Launching);
		return launchSuccess.Process;
	}

	private void OnLaunchSuccess(Task<InstanceProcess> task) {
		context.TransitionState(() => {
			context.ReportEvent(InstanceEvent.LaunchSucceded);
			
			var process = task.Result;
			var session = new InstanceSession(process, context);
			
			if (cancellationTokenSource.IsCancellationRequested) {
				return (new InstanceStoppingState(context, process, session), InstanceStatus.Stopping);
			}
			else {
				return (new InstanceRunningState(context, process, session), null);
			}
		});
	}

	private void OnLaunchFailure(Task task) {
		if (task.IsFaulted) {
			if (task.Exception is { InnerException: LaunchFailureException e }) {
				context.Logger.Error(e.LogMessage);
				context.SetLaunchFailedStatusAndReportEvent(e.Reason);
			}
			else {
				context.Logger.Error(task.Exception, "Caught exception while launching instance.");
				context.SetLaunchFailedStatusAndReportEvent(InstanceLaunchFailReason.UnknownError);
			}
		}

		context.Services.PortManager.Release(context.Configuration);
		context.TransitionState(new InstanceNotRunningState());
	}

	private sealed class LaunchFailureException : Exception {
		public InstanceLaunchFailReason Reason { get; }
		public string LogMessage { get; }

		public LaunchFailureException(InstanceLaunchFailReason reason, string logMessage) {
			this.Reason = reason;
			this.LogMessage = logMessage;
		}
	}

	public (IInstanceState, LaunchInstanceResult) Launch(InstanceContext context) {
		return (this, LaunchInstanceResult.InstanceAlreadyLaunching);
	}

	public (IInstanceState, StopInstanceResult) Stop(MinecraftStopStrategy stopStrategy) {
		cancellationTokenSource.Cancel();
		return (this, StopInstanceResult.StopInitiated);
	}

	public Task<bool> SendCommand(string command, CancellationToken cancellationToken) {
		return Task.FromResult(false);
	}

	public void Dispose() {
		cancellationTokenSource.Dispose();
	}
}
