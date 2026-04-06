using System.Diagnostics.CodeAnalysis;
using Phantom.Agent.Services.Instances;
using Phantom.Agent.Services.Instances.State;
using Phantom.Common.Data.Agent.Instance.Backups;
using Phantom.Common.Data.Backups;
using Phantom.Utils.Logging;
using Phantom.Utils.Tasks;

namespace Phantom.Agent.Services.Backups;

sealed class BackupScheduler : CancellableBackgroundTask {
	private readonly InstanceContext context;
	private readonly SemaphoreSlim backupSemaphore = new (initialCount: 1, maxCount: 1);
	
	private readonly TimeSpan initialDelay;
	private readonly TimeSpan backupInterval;
	private readonly TimeSpan failureRetryDelay;
	
	private readonly ManualResetEventSlim serverOutputWhileWaitingForOnlinePlayers = new ();
	private readonly InstancePlayerCountTracker? playerCountTracker;
	
	public event EventHandler<BackupCreationResult>? BackupCompleted;
	
	[SuppressMessage("ReSharper", "ConvertIfStatementToConditionalTernaryExpression")]
	public BackupScheduler(InstanceContext context, InstanceProcess process, InstanceBackupSchedule schedule) : base(PhantomLogger.Create<BackupScheduler>(context.ShortName)) {
		this.context = context;
		
		this.initialDelay = schedule.InitialDelay;
		this.backupInterval = schedule.BackupInterval;
		this.failureRetryDelay = schedule.BackupFailureRetryDelay;
		
		var playerCountDetectionStrategy = schedule.PlayerCountDetectionStrategy.Value;
		if (playerCountDetectionStrategy == null) {
			this.playerCountTracker = null;
		}
		else {
			this.playerCountTracker = new InstancePlayerCountTracker(context, process, playerCountDetectionStrategy.CreateDetector(new InstancePlayerCountDetectorFactory(context)));
		}
		
		Start();
	}
	
	protected override async Task RunTask() {
		await Task.Delay(initialDelay, CancellationToken);
		Logger.Information("Starting a new backup after server launched.");
		
		while (!CancellationToken.IsCancellationRequested) {
			var result = await CreateBackup();
			BackupCompleted?.Invoke(this, result);
			
			if (result.Kind.ShouldRetry()) {
				Logger.Warning("Scheduled backup failed, retrying in {Minutes} minutes.", failureRetryDelay.TotalMinutes);
				await Task.Delay(failureRetryDelay, CancellationToken);
			}
			else {
				Logger.Information("Scheduling next backup in {Minutes} minutes.", backupInterval.TotalMinutes);
				await Task.Delay(backupInterval, CancellationToken);
				
				if (playerCountTracker != null) {
					await WaitForOnlinePlayers(playerCountTracker);
				}
			}
		}
	}
	
	private async Task<BackupCreationResult> CreateBackup() {
		if (!await backupSemaphore.WaitAsync(TimeSpan.FromSeconds(1))) {
			return new BackupCreationResult(BackupCreationResultKind.BackupAlreadyRunning);
		}
		
		try {
			context.ActorCancellationToken.ThrowIfCancellationRequested();
			return await context.Actor.Request(new InstanceActor.BackupInstanceCommand(context.Services.BackupManager), context.ActorCancellationToken);
		} catch (OperationCanceledException) {
			return new BackupCreationResult(BackupCreationResultKind.InstanceNotRunning);
		} finally {
			backupSemaphore.Release();
		}
	}
	
	private async Task WaitForOnlinePlayers(InstancePlayerCountTracker playerCountTracker) {
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
		playerCountTracker?.Stop();
		backupSemaphore.Dispose();
		serverOutputWhileWaitingForOnlinePlayers.Dispose();
	}
}
