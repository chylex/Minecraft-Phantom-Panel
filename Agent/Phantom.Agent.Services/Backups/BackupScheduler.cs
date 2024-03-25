using Phantom.Agent.Minecraft.Instance;
using Phantom.Agent.Minecraft.Server;
using Phantom.Agent.Services.Instances;
using Phantom.Common.Data.Backups;
using Phantom.Utils.Logging;
using Phantom.Utils.Tasks;
using Phantom.Utils.Threading;

namespace Phantom.Agent.Services.Backups;

sealed class BackupScheduler : CancellableBackgroundTask {
	// TODO make configurable
	private static readonly TimeSpan InitialDelay = TimeSpan.FromMinutes(2);
	private static readonly TimeSpan BackupInterval = TimeSpan.FromMinutes(30);
	private static readonly TimeSpan BackupFailureRetryDelay = TimeSpan.FromMinutes(5);

	private readonly BackupManager backupManager;
	private readonly InstanceContext context;
	private readonly InstanceProcess process;
	private readonly SemaphoreSlim backupSemaphore = new (1, 1);
	private readonly int serverPort;
	private readonly ServerStatusProtocol serverStatusProtocol;
	private readonly ManualResetEventSlim serverOutputWhileWaitingForOnlinePlayers = new ();
	
	public event EventHandler<BackupCreationResult>? BackupCompleted;

	public BackupScheduler(InstanceContext context, InstanceProcess process, int serverPort) : base(PhantomLogger.Create<BackupScheduler>(context.ShortName), context.Services.TaskManager, "Backup scheduler for " + context.ShortName) {
		this.backupManager = context.Services.BackupManager;
		this.context = context;
		this.process = process;
		this.serverPort = serverPort;
		this.serverStatusProtocol = new ServerStatusProtocol(context.ShortName);
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
		bool needsToLogOfflinePlayersMessage = true;
		
		process.AddOutputListener(ServerOutputListener, maxLinesToReadFromHistory: 0);
		try {
			while (!CancellationToken.IsCancellationRequested) {
				serverOutputWhileWaitingForOnlinePlayers.Reset();
				
				var onlinePlayerCount = await serverStatusProtocol.GetOnlinePlayerCount(serverPort, CancellationToken);
				if (onlinePlayerCount == null) {
					Logger.Warning("Could not detect whether any players are online, starting a new backup.");
					break;
				}

				if (onlinePlayerCount > 0) {
					Logger.Information("Players are online, starting a new backup.");
					break;
				}

				if (needsToLogOfflinePlayersMessage) {
					needsToLogOfflinePlayersMessage = false;
					Logger.Information("No players are online, waiting for someone to join before starting a new backup.");
				}

				await Task.Delay(TimeSpan.FromSeconds(10), CancellationToken);
				
				Logger.Debug("Waiting for server output before checking for online players again...");
				await serverOutputWhileWaitingForOnlinePlayers.WaitHandle.WaitOneAsync(CancellationToken);
			}
		} finally {
			process.RemoveOutputListener(ServerOutputListener);
		}
	}

	private void ServerOutputListener(object? sender, string line) {
		if (!serverOutputWhileWaitingForOnlinePlayers.IsSet) {
			serverOutputWhileWaitingForOnlinePlayers.Set();
			Logger.Debug("Detected server output, signalling to check for online players again.");
		}
	}

	protected override void Dispose() {
		backupSemaphore.Dispose();
		serverOutputWhileWaitingForOnlinePlayers.Dispose();
	}
}
