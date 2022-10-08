using Phantom.Agent.Minecraft.Command;
using Phantom.Agent.Minecraft.Instance;
using Phantom.Common.Data.Instance;
using Phantom.Common.Data.Minecraft;
using Phantom.Common.Data.Replies;

namespace Phantom.Agent.Services.Instances.States;

sealed class InstanceRunningState : IInstanceState {
	private readonly InstanceContext context;
	private readonly InstanceSession session;
	private readonly InstanceLogSenderThread logSenderThread;
	private readonly SessionObjects sessionObjects;
	
	private readonly CancellationTokenSource delayedStopCancellationTokenSource = new ();
	private bool stateOwnsDelayedStopCancellationTokenSource = true;
	private bool isStopping;

	public InstanceRunningState(InstanceContext context, InstanceSession session) {
		this.context = context;
		this.session = session;
		this.logSenderThread = new InstanceLogSenderThread(context.Configuration.InstanceGuid, context.ShortName);
		this.sessionObjects = new SessionObjects(this);
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

	public (IInstanceState, StopInstanceResult) Stop(MinecraftStopStrategy stopStrategy) {
		if (stopStrategy == MinecraftStopStrategy.Instant) {
			CancelDelayedStop();
			return (PrepareStoppedState(), StopInstanceResult.StopInitiated);
		}

		if (isStopping) {
			// TODO change delay or something
			return (this, StopInstanceResult.InstanceAlreadyStopping);
		}
		
		isStopping = true;
		context.LaunchServices.TaskManager.Run(() => StopLater(stopStrategy.Seconds));
		return (this, StopInstanceResult.StopInitiated);
	}

	private IInstanceState PrepareStoppedState() {
		session.SessionEnded -= SessionEnded;
		return new InstanceStoppingState(context, session, sessionObjects);
	}

	private void CancelDelayedStop() {
		try {
			delayedStopCancellationTokenSource.Cancel();
		} catch (ObjectDisposedException) {
			// ignore
		}
	}

	private async Task StopLater(int seconds) {
		var cancellationToken = delayedStopCancellationTokenSource.Token;

		try {
			stateOwnsDelayedStopCancellationTokenSource = false;

			int[] stops = { 60, 30, 10, 5, 4, 3, 2, 1, 0 };
			
			foreach (var stop in stops) {
				if (seconds > stop) {
					await SendCommand(MinecraftCommand.Say("Server shutting down in " + seconds + (seconds == 1 ? " second." : " seconds.")), cancellationToken);
					await Task.Delay(TimeSpan.FromSeconds(seconds - stop), cancellationToken);
					seconds = stop;
				}
			}
		} catch (OperationCanceledException) {
			context.Logger.Verbose("Cancelled delayed stop.");
			return;
		} catch (ObjectDisposedException) {
			return;
		} catch (Exception e) {
			context.Logger.Warning(e, "Caught exception during delayed stop.");
			return;
		} finally {
			delayedStopCancellationTokenSource.Dispose();
		}

		context.TransitionState(PrepareStoppedState());
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
		private readonly InstanceRunningState state;
		private bool isDisposed;

		public SessionObjects(InstanceRunningState state) {
			this.state = state;
		}

		public bool Dispose() {
			lock (this) {
				if (isDisposed) {
					return false;
				}

				isDisposed = true;
			}

			if (state.stateOwnsDelayedStopCancellationTokenSource) {
				state.delayedStopCancellationTokenSource.Dispose();
			}
			else {
				state.CancelDelayedStop();
			}
			
			state.logSenderThread.Cancel();
			state.session.Dispose();
			state.context.PortManager.Release(state.context.Configuration);
			return true;
		}
	}
}
