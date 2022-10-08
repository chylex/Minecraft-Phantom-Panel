using Phantom.Agent.Minecraft.Instance;
using Phantom.Common.Data.Instance;
using Phantom.Common.Data.Replies;

namespace Phantom.Agent.Services.Instances.States;

sealed class InstanceRunningState : IInstanceState {
	private readonly InstanceContext context;
	private readonly InstanceSession session;
	private readonly InstanceLogSenderThread logSenderThread;
	private readonly SessionObjects sessionObjects;

	public InstanceRunningState(InstanceContext context, InstanceSession session) {
		this.context = context;
		this.session = session;
		this.logSenderThread = new InstanceLogSenderThread(context.Configuration.InstanceGuid, context.ShortName);
		this.sessionObjects = new SessionObjects(context, session, logSenderThread);
	}

	public void Initialize() {
		session.AddOutputListener(SessionOutput);
		session.SessionEnded += SessionEnded;
		
		if (session.HasEnded) {
			if (sessionObjects.Dispose()) {
				context.Logger.Warning("Session ended immediately after it was started.");
				context.ReportStatus(new InstanceStatus.Failed(InstanceLaunchFailReason.UnknownError));
				context.LaunchServices.TaskManager.Run(() => context.TransitionState(new InstanceNotRunningState()));
			}
		}
		else {
			context.ReportStatus(InstanceStatus.IsRunning);
			context.Logger.Information("Session started.");
		}
	}

	private void SessionOutput(object? sender, string e) {
		context.Logger.Verbose("[Server] {Line}", e);
		logSenderThread.Enqueue(e);
	}

	private void SessionEnded(object? sender, EventArgs e) {
		if (sessionObjects.Dispose()) {
			context.Logger.Information("Session ended.");
			context.ReportStatus(InstanceStatus.IsNotRunning);
			context.TransitionState(new InstanceNotRunningState());
		}
	}

	public (IInstanceState, LaunchInstanceResult) Launch(InstanceContext context) {
		return (this, LaunchInstanceResult.InstanceAlreadyRunning);
	}

	public (IInstanceState, StopInstanceResult) Stop() {
		session.SessionEnded -= SessionEnded;
		return (new InstanceStoppingState(context, session, sessionObjects), StopInstanceResult.StopInitiated);
	}

	public async Task<bool> SendCommand(string command, CancellationToken cancellationToken) {
		try {
			context.Logger.Information("Sending command: {Command}", command);
			await session.SendCommand(command, cancellationToken);
			return true;
		} catch (OperationCanceledException) {
			return false;
		} catch (Exception e) {
			context.Logger.Warning(e, "Caught exception while sending command.");
			return false;
		}
	}

	public sealed class SessionObjects {
		private readonly InstanceContext context;
		private readonly InstanceSession session;
		private readonly InstanceLogSenderThread logSenderThread;
		private bool isDisposed;

		public SessionObjects(InstanceContext context, InstanceSession session, InstanceLogSenderThread logSenderThread) {
			this.context = context;
			this.session = session;
			this.logSenderThread = logSenderThread;
		}

		public bool Dispose() {
			lock (this) {
				if (isDisposed) {
					return false;
				}

				isDisposed = true;
			}

			logSenderThread.Cancel();
			session.Dispose();
			context.PortManager.Release(context.Configuration);
			return true;
		}
	}
}
