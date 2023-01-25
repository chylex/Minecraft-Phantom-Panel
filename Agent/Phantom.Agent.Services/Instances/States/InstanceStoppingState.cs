using System.Diagnostics;
using Phantom.Agent.Minecraft.Command;
using Phantom.Agent.Minecraft.Instance;
using Phantom.Common.Data.Instance;
using Phantom.Common.Data.Minecraft;
using Phantom.Common.Data.Replies;

namespace Phantom.Agent.Services.Instances.States; 

sealed class InstanceStoppingState : IInstanceState, IDisposable {
	private readonly InstanceContext context;
	private readonly InstanceSession session;
	private readonly InstanceRunningState.SessionObjects sessionObjects;

	public InstanceStoppingState(InstanceContext context, InstanceSession session, InstanceRunningState.SessionObjects sessionObjects) {
		this.sessionObjects = sessionObjects;
		this.session = session;
		this.context = context;
	}

	public void Initialize() {
		context.Logger.Information("Session stopping.");
		context.ReportStatus(InstanceStatus.Stopping);
		context.LaunchServices.TaskManager.Run("Stop procedure for instance " + context.ShortName, DoStop);
	}

	private async Task DoStop() {
		try {
			context.Logger.Information("Sending stop command...");
			await DoSendStopCommand();

			context.Logger.Information("Waiting for session to end...");
			await DoWaitForSessionToEnd();
		} finally {
			context.Logger.Information("Session stopped.");
			context.TransitionState(new InstanceNotRunningState(), InstanceStatus.NotRunning);
		}
	}

	private async Task DoSendStopCommand() {
		using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
		try {
			await session.SendCommand(MinecraftCommand.Stop, timeout.Token);
		} catch (OperationCanceledException) {
			// ignore
		} catch (ObjectDisposedException e) when (e.ObjectName == typeof(Process).FullName && session.HasEnded) {
			// ignore
		} catch (Exception e) {
			context.Logger.Warning(e, "Caught exception while sending stop command.");
		}
	}

	private async Task DoWaitForSessionToEnd() {
		using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(55));
		try {
			await session.WaitForExit(timeout.Token);
		} catch (OperationCanceledException) {
			try {
				context.Logger.Warning("Waiting timed out, killing session...");
				session.Kill();
			} catch (Exception e) {
				context.Logger.Error(e, "Caught exception while killing session.");
			}
		}
	}

	public (IInstanceState, LaunchInstanceResult) Launch(InstanceContext context) {
		return (this, LaunchInstanceResult.InstanceIsStopping);
	}

	public (IInstanceState, StopInstanceResult) Stop(MinecraftStopStrategy stopStrategy) {
		return (this, StopInstanceResult.InstanceAlreadyStopping); // TODO maybe provide a way to kill?
	}

	public Task<bool> SendCommand(string command, CancellationToken cancellationToken) {
		return Task.FromResult(false);
	}

	public void Dispose() {
		sessionObjects.Dispose();
	}
}
