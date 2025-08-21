using Phantom.Agent.Services.Instances;
using Phantom.Agent.Services.Instances.State;
using Phantom.Common.Data.Backups;
using Phantom.Utils.Logging;
using Phantom.Utils.Tasks;

namespace Phantom.Agent.Services.Backups;

sealed class BackupScheduler : CancellableBackgroundTask {
	// TODO make configurable
	private static readonly TimeSpan InitialDelay = TimeSpan.FromMinutes(2);
	private static readonly TimeSpan BackupInterval = TimeSpan.FromMinutes(30);
	private static readonly TimeSpan BackupFailureRetryDelay = TimeSpan.FromMinutes(5);
	
	private readonly BackupManager backupManager;
	private readonly InstanceContext context;
	private readonly SemaphoreSlim backupSemaphore = new (1, 1);
	private readonly ManualResetEventSlim serverOutputWhileWaitingForOnlinePlayers = new ();
	private readonly InstancePlayerCountTracker playerCountTracker;
	
	public event EventHandler<BackupCreationResult>? BackupCompleted;
	
	public BackupScheduler(InstanceContext context, InstancePlayerCountTracker playerCountTracker) : base(PhantomLogger.Create<BackupScheduler>(context.ShortName)) {
		this.backupManager = context.Services.BackupManager;
		this.context = context;
		this.playerCountTracker = playerCountTracker;
		Start();
	}
	
	protected override async Task RunTask() {
		await Task.Delay(InitialDelay, CancellationToken);
		Logger.Information("Starting a new backup after server launched.");
		
		while (!CancellationToken.IsCancellationRequested) {
			var result = await CreateBackup();
			BackupCompleted?.Invoke(this, result);
			
			if (result.Kind.ShouldRetry()) {
				Logger.Warning("Scheduled backup failed, retrying in {Minutes} minutes.", BackupFailureRetryDelay.TotalMinutes);
				await Task.Delay(BackupFailureRetryDelay, CancellationToken);
			}
			else {
				Logger.Information("Scheduling next backup in {Minutes} minutes.", BackupInterval.TotalMinutes);
				await Task.Delay(BackupInterval, CancellationToken);
				await WaitForOnlinePlayers();
			}
		}
	}
	
	private async Task<BackupCreationResult> CreateBackup() {
		if (!await backupSemaphore.WaitAsync(TimeSpan.FromSeconds(1))) {
			return new BackupCreationResult(BackupCreationResultKind.BackupAlreadyRunning);
		}
		
		try {
			context.ActorCancellationToken.ThrowIfCancellationRequested();
			return await context.Actor.Request(new InstanceActor.BackupInstanceCommand(backupManager), context.ActorCancellationToken);
		} catch (OperationCanceledException) {
			return new BackupCreationResult(BackupCreationResultKind.InstanceNotRunning);
		} finally {
			backupSemaphore.Release();
		}
	}
	
	private async Task WaitForOnlinePlayers() {
		var task = playerCountTracker.WaitForOnlinePlayers(CancellationToken);
		if (!task.IsCompleted) {
			Logger.Information("Waiting for someone to join before starting a new backup.");
		}
		
		try {
			await task;
			Logger.Information("Players are online, starting a new backup.");
		} catch (OperationCanceledException) {
			throw;
		} catch (Exception) {
			Logger.Warning("Could not detect whether any players are online, starting a new backup.");
		}
	}
	
	protected override void Dispose() {
		backupSemaphore.Dispose();
		serverOutputWhileWaitingForOnlinePlayers.Dispose();
	}
}
