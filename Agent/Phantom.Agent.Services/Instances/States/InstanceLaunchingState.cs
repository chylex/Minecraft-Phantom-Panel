using Phantom.Agent.Minecraft.Instance;
using Phantom.Agent.Minecraft.Launcher;
using Phantom.Common.Data.Instance;

namespace Phantom.Agent.Services.Instances.States;

sealed class InstanceLaunchingState : IInstanceState, IDisposable {
	private readonly InstanceContext context;
	private readonly CancellationTokenSource cancellationTokenSource = new ();

	public InstanceLaunchingState(InstanceContext context) {
		this.context = context;
		this.context.Logger.Information("Session starting...");
		this.context.ReportStatus(new InstanceStatus.Downloading(0)); // TODO
		
		var launchTask = Task.Run(DoLaunch);
		launchTask.ContinueWith(OnLaunchSuccess, TaskContinuationOptions.OnlyOnRanToCompletion);
		launchTask.ContinueWith(OnLaunchFailure, TaskContinuationOptions.NotOnRanToCompletion);
	}

	private async Task<InstanceSession> DoLaunch() {
		var cancellationToken = cancellationTokenSource.Token;
		cancellationToken.ThrowIfCancellationRequested();

		context.ReportStatus(new InstanceStatus.Downloading(100)); // TODO

		var launchResult = await context.Launcher.Launch(context.LaunchServices, cancellationToken);
		if (launchResult is LaunchResult.CouldNotDownloadMinecraftServer) {
			throw new LaunchFailureException(InstanceLaunchFailReason.CouldNotDownloadMinecraftServer, "Session failed to launch, could not download Minecraft server.");
		}
		else if (launchResult is LaunchResult.InvalidJavaRuntime) {
			throw new LaunchFailureException(InstanceLaunchFailReason.JavaRuntimeNotFound, "Session failed to launch, invalid Java runtime.");
		}

		if (launchResult is not LaunchResult.Success launchSuccess) {
			throw new LaunchFailureException(InstanceLaunchFailReason.UnknownError, "Session failed to launch.");
		}

		return launchSuccess.Session;
	}

	private void OnLaunchSuccess(Task<InstanceSession> task) {
		context.TransitionState(() => {
			if (cancellationTokenSource.IsCancellationRequested) {
				context.PortManager.Release(context.Configuration);
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

	public IInstanceState Stop(InstanceContext context) {
		cancellationTokenSource.Cancel();
		return this;
	}

	public Task<bool> SendCommand(string command, CancellationToken cancellationToken) {
		return Task.FromResult(false);
	}

	public void Dispose() {
		cancellationTokenSource.Dispose();
	}
}
