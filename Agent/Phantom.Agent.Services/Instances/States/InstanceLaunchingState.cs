using Phantom.Agent.Minecraft.Instance;
using Phantom.Agent.Minecraft.Launcher;
using Phantom.Agent.Minecraft.Server;
using Phantom.Common.Data.Instance;

namespace Phantom.Agent.Services.Instances.States;

sealed class InstanceLaunchingState : IInstanceState, IDisposable {
	private readonly InstanceContext context;
	private readonly CancellationTokenSource cancellationTokenSource = new ();
	private byte lastDownloadProgress = byte.MaxValue;

	public InstanceLaunchingState(InstanceContext context) {
		this.context = context;
		this.context.Logger.Information("Session starting...");
		this.context.ReportStatus(InstanceStatus.IsLaunching);
		
		var launchTask = Task.Run(DoLaunch);
		launchTask.ContinueWith(OnLaunchSuccess, CancellationToken.None, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Default);
		launchTask.ContinueWith(OnLaunchFailure, CancellationToken.None, TaskContinuationOptions.NotOnRanToCompletion, TaskScheduler.Default);
	}

	private async Task<InstanceSession> DoLaunch() {
		var cancellationToken = cancellationTokenSource.Token;
		cancellationToken.ThrowIfCancellationRequested();
		
		void OnDownloadProgress(object? sender, DownloadProgressEventArgs args) {
			byte progress = (byte) Math.Min(args.DownloadedBytes * 100 / args.TotalBytes, 100);
			
			if (lastDownloadProgress != progress) {
				lastDownloadProgress = progress;
				context.ReportStatus(new InstanceStatus.Downloading(progress));
			}
		}

		var launchResult = await context.Launcher.Launch(context.LaunchServices, OnDownloadProgress, cancellationToken);
		if (launchResult is LaunchResult.CouldNotDownloadMinecraftServer) {
			throw new LaunchFailureException(InstanceLaunchFailReason.CouldNotDownloadMinecraftServer, "Session failed to launch, could not download Minecraft server.");
		}
		else if (launchResult is LaunchResult.InvalidJavaRuntime) {
			throw new LaunchFailureException(InstanceLaunchFailReason.JavaRuntimeNotFound, "Session failed to launch, invalid Java runtime.");
		}

		if (launchResult is not LaunchResult.Success launchSuccess) {
			throw new LaunchFailureException(InstanceLaunchFailReason.UnknownError, "Session failed to launch.");
		}

		context.ReportStatus(InstanceStatus.IsLaunching);
		return launchSuccess.Session;
	}

	private void OnLaunchSuccess(Task<InstanceSession> task) {
		context.TransitionState(() => {
			if (cancellationTokenSource.IsCancellationRequested) {
				context.PortManager.Release(context.Configuration);
				context.ReportStatus(InstanceStatus.IsNotRunning);
				return new InstanceNotRunningState();
			}
			else {
				return new InstanceRunningState(context, task.Result);
			}
		});
	}

	private void OnLaunchFailure(Task task) {
		if (task.Exception is { InnerException: LaunchFailureException e }) {
			context.Logger.Error(e.LogMessage);
			context.ReportStatus(new InstanceStatus.Failed(e.Reason));
		}
		else {
			context.ReportStatus(new InstanceStatus.Failed(InstanceLaunchFailReason.UnknownError));
		}
		
		context.PortManager.Release(context.Configuration);
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

	public IInstanceState Launch(InstanceContext context) {
		return this;
	}

	public IInstanceState Stop() {
		cancellationTokenSource.Cancel();
		return this;
	}

	public void Dispose() {
		cancellationTokenSource.Dispose();
	}
}
