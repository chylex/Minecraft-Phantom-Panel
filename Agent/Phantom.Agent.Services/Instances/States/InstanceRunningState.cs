using Phantom.Agent.Minecraft.Instance;
using Phantom.Agent.Minecraft.Launcher;
using Phantom.Agent.Services.Backups;
using Phantom.Agent.Services.Instances.Procedures;
using Phantom.Common.Data.Backups;
using Phantom.Common.Data.Instance;

namespace Phantom.Agent.Services.Instances.States;

sealed class InstanceRunningState : IInstanceState, IDisposable {
	public InstanceProcess Process { get; }

	internal bool IsStopping { get; set; }

	private readonly InstanceConfiguration configuration;
	private readonly IServerLauncher launcher;
	private readonly IInstanceContext context;

	private readonly InstanceLogSender logSender;
	private readonly BackupScheduler backupScheduler;

	private bool isDisposed;

	public InstanceRunningState(InstanceConfiguration configuration, IServerLauncher launcher, InstanceProcess process, IInstanceContext context) {
		this.configuration = configuration;
		this.launcher = launcher;
		this.context = context;
		this.Process = process;

		this.logSender = new InstanceLogSender(context.Services.ControllerConnection, context.Services.TaskManager, configuration.InstanceGuid, context.ShortName);

		this.backupScheduler = new BackupScheduler(context.Services.TaskManager, context.Services.BackupManager, process, context, configuration.ServerPort);
		this.backupScheduler.BackupCompleted += OnScheduledBackupCompleted;
	}

	public void Initialize() {
		Process.Ended += ProcessEnded;

		if (Process.HasEnded) {
			if (TryDispose()) {
				context.Logger.Warning("Session ended immediately after it was started.");
				context.EnqueueProcedure(new SetInstanceToNotRunningStateProcedure(InstanceStatus.Failed(InstanceLaunchFailReason.UnknownError)), immediate: true);
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

		if (IsStopping) {
			context.EnqueueProcedure(new SetInstanceToNotRunningStateProcedure(InstanceStatus.NotRunning), immediate: true);
		}
		else {
			context.Logger.Information("Session ended unexpectedly, restarting...");
			context.ReportEvent(InstanceEvent.Crashed);
			context.EnqueueProcedure(new LaunchInstanceProcedure(configuration, launcher, IsRestarting: true));
		}
	}

	private void OnScheduledBackupCompleted(object? sender, BackupCreationResult e) {
		context.ReportEvent(new InstanceBackupCompletedEvent(e.Kind, e.Warnings));
	}

	public async Task<bool> SendCommand(string command, CancellationToken cancellationToken) {
		try {
			context.Logger.Information("Sending command: {Command}", command);
			await Process.SendCommand(command, cancellationToken);
			return true;
		} catch (OperationCanceledException) {
			return false;
		} catch (Exception e) {
			context.Logger.Warning(e, "Caught exception while sending command.");
			return false;
		}
	}

	private bool TryDispose() {
		lock (this) {
			if (isDisposed) {
				return false;
			}

			isDisposed = true;
		}

		logSender.Stop();
		backupScheduler.Stop();
		
		Process.Dispose();
		context.Services.PortManager.Release(configuration);
		
		return true;
	}

	public void Dispose() {
		TryDispose();
	}
}
