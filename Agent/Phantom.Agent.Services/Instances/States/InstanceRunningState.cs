using Phantom.Agent.Minecraft.Command;
using Phantom.Agent.Minecraft.Instance;
using Phantom.Agent.Services.Backups;
using Phantom.Common.Data.Backups;
using Phantom.Common.Data.Instance;
using Phantom.Common.Data.Minecraft;
using Phantom.Common.Data.Replies;

namespace Phantom.Agent.Services.Instances.States;

sealed class InstanceRunningState : IInstanceState {
	private readonly InstanceContext context;
	private readonly InstanceSession session;
	private readonly InstanceLogSender logSender;
	private readonly BackupScheduler backupScheduler;
	private readonly SessionObjects sessionObjects;
	
	private readonly CancellationTokenSource delayedStopCancellationTokenSource = new ();
	private bool stateOwnsDelayedStopCancellationTokenSource = true;
	private bool isStopping;

	public InstanceRunningState(InstanceContext context, InstanceSession session) {
		this.context = context;
		this.session = session;
		this.logSender = new InstanceLogSender(context.Services.TaskManager, context.Configuration.InstanceGuid, context.ShortName);
		this.backupScheduler = new BackupScheduler(context.Services.TaskManager, context.Services.BackupManager, session, context.Configuration.ServerPort, context.ShortName);
		this.backupScheduler.BackupCompleted += OnScheduledBackupCompleted;
		this.sessionObjects = new SessionObjects(this);
	}

	public void Initialize() {
		session.AddOutputListener(SessionOutput);
		session.SessionEnded += SessionEnded;
		
		if (session.HasEnded) {
			if (sessionObjects.Dispose()) {
				context.Logger.Warning("Session ended immediately after it was started.");
				context.ReportEvent(InstanceEvent.Stopped);
				context.Services.TaskManager.Run("Transition state of instance " + context.ShortName + " to not running", () => context.TransitionState(new InstanceNotRunningState(), InstanceStatus.Failed(InstanceLaunchFailReason.UnknownError)));
			}
		}
		else {
			context.SetStatus(InstanceStatus.Running);
			context.Logger.Information("Session started.");
		}
	}

	private void SessionOutput(object? sender, string line) {
		context.Logger.Verbose("[Server] {Line}", line);
		logSender.Enqueue(line);
	}

	private void SessionEnded(object? sender, EventArgs e) {
		if (!sessionObjects.Dispose()) {
			return;
		}

		if (isStopping) {
			context.Logger.Information("Session ended.");
			context.ReportEvent(InstanceEvent.Stopped);
			context.TransitionState(new InstanceNotRunningState(), InstanceStatus.NotRunning);
		}
		else {
			context.Logger.Information("Session ended unexpectedly, restarting...");
			context.ReportEvent(InstanceEvent.Crashed);
			context.TransitionState(new InstanceLaunchingState(context), InstanceStatus.Restarting);
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
		context.Services.TaskManager.Run("Delayed stop timer for instance " + context.ShortName, () => StopLater(stopStrategy.Seconds));
		return (this, StopInstanceResult.StopInitiated);
	}

	private IInstanceState PrepareStoppedState() {
		session.SessionEnded -= SessionEnded;
		backupScheduler.Stop();
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

	private void OnScheduledBackupCompleted(object? sender, BackupCreationResult e) {
		context.ReportEvent(new InstanceBackupCompletedEvent(e.Kind, e.Warnings));
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
			
			state.logSender.Stop();
			state.session.Dispose();
			state.context.Services.PortManager.Release(state.context.Configuration);
			return true;
		}
	}
}
