using Phantom.Agent.Minecraft.Instance;
using Phantom.Common.Data.Instance;

namespace Phantom.Agent.Services.Instances.States; 

sealed class InstanceStoppingState : IInstanceState, IDisposable {
	private readonly InstanceContext context;
	private readonly InstanceSession session;
	private readonly InstanceRunningState.SessionObjects sessionObjects;

	public InstanceStoppingState(InstanceContext context, InstanceSession session, InstanceRunningState.SessionObjects sessionObjects) {
		this.sessionObjects = sessionObjects;
		this.session = session;
		this.context = context;
		this.context.Logger.Information("Session stopping.");
		this.context.ReportStatus(InstanceStatus.IsStopping);
		
		context.LaunchServices.TaskManager.Run(DoStop);
	}

	private async Task DoStop() {
		try {
			context.Logger.Information("Sending stop command...");
			await DoSendStopCommand();

			context.Logger.Information("Waiting for session to end...");
			await DoWaitForSessionToEnd();
		} finally {
			context.Logger.Information("Session stopped.");
			context.ReportStatus(InstanceStatus.IsNotRunning);
			context.TransitionState(new InstanceNotRunningState());
		}
	}

	private async Task DoSendStopCommand() {
		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
		try {
			await session.SendCommand("stop", cts.Token);
		} catch (OperationCanceledException) {
			// ignore
		} catch (Exception e) {
			context.Logger.Warning(e, "Caught exception while sending stop command.");
		}
	}

	private async Task DoWaitForSessionToEnd() {
		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(55));
		try {
			await session.WaitForExit(cts.Token);
		} catch (OperationCanceledException) {
			try {
				context.Logger.Warning("Waiting timed out, killing session...");
				session.Kill();
			} catch (Exception e) {
				context.Logger.Error(e, "Caught exception while killing session.");
			}
		}
	}

	public IInstanceState Launch(InstanceContext context) {
		return this;
	}

	public IInstanceState Stop() {
		return this; // TODO maybe provide a way to kill?
	}

	public Task<bool> SendCommand(string command, CancellationToken cancellationToken) {
		return Task.FromResult(false);
	}

	public void Dispose() {
		sessionObjects.Dispose();
	}
}
