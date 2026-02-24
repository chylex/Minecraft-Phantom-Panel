using Phantom.Agent.Services.Backups;
using Phantom.Agent.Services.Instances.Launch;
using Phantom.Common.Data.Agent.Instance;
using Phantom.Common.Data.Backups;
using Phantom.Common.Data.Instance;
using Phantom.Common.Data.Replies;

namespace Phantom.Agent.Services.Instances.State;

sealed class InstanceRunningState : IDisposable {
	public InstanceTicketManager.Ticket Ticket { get; }
	public InstanceProcess Process { get; }
	
	internal IInstanceValueResolver ValueResolver => launcher.ValueResolver;
	internal bool IsStopping { get; set; }
	
	private readonly InstanceContext context;
	private readonly InstanceInfo info;
	private readonly InstanceLauncher launcher;
	private readonly CancellationToken cancellationToken;
	
	private readonly InstanceLogSender logSender;
	private readonly InstancePlayerCountTracker playerCountTracker;
	private readonly BackupScheduler backupScheduler;
	
	private bool isDisposed;
	
	public InstanceRunningState(InstanceContext context, InstanceInfo info, InstanceLauncher launcher, InstanceTicketManager.Ticket ticket, InstanceProcess process, CancellationToken cancellationToken) {
		this.context = context;
		this.info = info;
		this.launcher = launcher;
		this.Ticket = ticket;
		this.Process = process;
		this.cancellationToken = cancellationToken;
		
		this.logSender = new InstanceLogSender(context.Services.ControllerConnection, context.InstanceGuid, context.ShortName);
		this.playerCountTracker = new InstancePlayerCountTracker(context, process, info.ServerPort);
		
		this.backupScheduler = new BackupScheduler(context, playerCountTracker);
		this.backupScheduler.BackupCompleted += OnScheduledBackupCompleted;
	}
	
	public void Initialize() {
		Process.Ended += ProcessEnded;
		
		if (Process.HasEnded) {
			if (TryDispose()) {
				context.Logger.Warning("Session ended immediately after it was started.");
				context.Actor.Tell(new InstanceActor.HandleProcessEndedCommand(InstanceStatus.Failed(InstanceLaunchFailReason.UnknownError)));
			}
		}
		else {
			context.Logger.Information("Session started.");
			Process.AddOutputListener(SessionOutput);
		}
	}
	
	private void SessionOutput(object? sender, string line) {
		context.Logger.Debug("[Server] {Line}", line);
		logSender.Enqueue(line);
	}
	
	private void ProcessEnded(object? sender, EventArgs e) {
		if (!TryDispose()) {
			return;
		}
		
		if (cancellationToken.IsCancellationRequested) {
			return;
		}
		
		if (IsStopping) {
			context.Actor.Tell(new InstanceActor.HandleProcessEndedCommand(InstanceStatus.NotRunning));
		}
		else {
			context.Logger.Information("Session ended unexpectedly, restarting...");
			context.ReportEvent(InstanceEvent.Crashed);
			context.Actor.Tell(new InstanceActor.LaunchInstanceCommand(info, launcher, Ticket, IsRestarting: true));
		}
	}
	
	private void OnScheduledBackupCompleted(object? sender, BackupCreationResult e) {
		context.ReportEvent(new InstanceBackupCompletedEvent(e.Kind, e.Warnings));
	}
	
	public async Task<SendCommandToInstanceResult> SendCommand(string command, CancellationToken cancellationToken) {
		try {
			context.Logger.Information("Sending command: {Command}", command);
			await Process.SendCommand(command, cancellationToken);
			return SendCommandToInstanceResult.Success;
		} catch (OperationCanceledException) {
			return SendCommandToInstanceResult.UnknownError;
		} catch (Exception e) {
			context.Logger.Warning(e, "Caught exception while sending command.");
			return SendCommandToInstanceResult.UnknownError;
		}
	}
	
	public void OnStopInitiated() {
		backupScheduler.Stop();
		playerCountTracker.Stop();
	}
	
	private bool TryDispose() {
		lock (this) {
			if (isDisposed) {
				return false;
			}
			
			isDisposed = true;
		}
		
		OnStopInitiated();
		logSender.Stop();
		
		Process.Dispose();
		
		return true;
	}
	
	public void Dispose() {
		TryDispose();
	}
}
