using Phantom.Agent.Minecraft.Instance;
using Phantom.Common.Data.Instance;

namespace Phantom.Agent.Services.Instances.States;

sealed class InstanceRunningState : IInstanceState {
	private readonly InstanceContext context;
	private readonly InstanceSession session;
	private readonly SessionObjects sessionObjects;

	public InstanceRunningState(InstanceContext context, InstanceSession session) {
		this.context = context;
		this.session = session;
		this.sessionObjects = new SessionObjects(context, session);

		this.session.AddOutputListener(SessionOutput);
		this.session.SessionEnded += SessionEnded;

		if (session.HasEnded) {
			if (sessionObjects.Dispose()) {
				context.Logger.Warning("Session ended immediately after it was started.");
				context.ReportStatus(new InstanceStatus.Failed(InstanceLaunchFailReason.UnknownError));
				Task.Run(() => context.TransitionState(new InstanceNotRunningState()));
			}
		}
		else {
			context.ReportStatus(InstanceStatus.IsRunning);
			context.Logger.Information("Session started.");
		}
	}

	private void SessionOutput(object? sender, string e) {
		context.Logger.Verbose("[Server] {Line}", e);
	}

	private void SessionEnded(object? sender, EventArgs e) {
		if (sessionObjects.Dispose()) {
			context.Logger.Information("Session ended.");
			context.ReportStatus(InstanceStatus.IsNotRunning);
			context.TransitionState(new InstanceNotRunningState());
		}
	}

	public IInstanceState Launch(InstanceContext context) {
		return this;
	}

	public IInstanceState Stop() {
		session.SessionEnded -= SessionEnded;
		return new InstanceStoppingState(context, session, sessionObjects);
	}

	public sealed class SessionObjects {
		private readonly InstanceContext context;
		private readonly InstanceSession session;
		private bool isDisposed;

		public SessionObjects(InstanceContext context, InstanceSession session) {
			this.context = context;
			this.session = session;
		}

		public bool Dispose() {
			lock (this) {
				if (isDisposed) {
					return false;
				}

				isDisposed = true;
			}

			session.Dispose();
			context.PortManager.Release(context.Configuration);
			return true;
		}
	}
}
