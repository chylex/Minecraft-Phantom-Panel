using Phantom.Agent.Minecraft.Instance;
using Phantom.Agent.Minecraft.Server;
using Phantom.Common.Data.Backups;
using Phantom.Common.Logging;
using Phantom.Utils.Runtime;

namespace Phantom.Agent.Services.Backups;

sealed class BackupScheduler : CancellableBackgroundTask {
	// TODO make configurable
	private static readonly TimeSpan InitialDelay = TimeSpan.FromMinutes(2);
	private static readonly TimeSpan BackupInterval = TimeSpan.FromMinutes(30);
	private static readonly TimeSpan BackupFailureRetryDelay = TimeSpan.FromMinutes(5);

	private readonly string loggerName;
	private readonly BackupManager backupManager;
	private readonly InstanceSession session;
	private readonly int serverPort;
	private readonly ServerStatusProtocol serverStatusProtocol;
	private readonly ManualResetEventSlim serverOutputWhileWaitingForOnlinePlayers = new ();

	public BackupScheduler(TaskManager taskManager, BackupManager backupManager, InstanceSession session, int serverPort, string loggerName) : base(PhantomLogger.Create<BackupScheduler>(loggerName), taskManager, "Backup scheduler for " + loggerName) {
		this.loggerName = loggerName;
		this.backupManager = backupManager;
		this.session = session;
		this.serverPort = serverPort;
		this.serverStatusProtocol = new ServerStatusProtocol(loggerName);
	}

	protected override async Task RunTask() {
		await Task.Delay(InitialDelay, CancellationToken);
		Logger.Information("Starting a new backup after server launched.");
			
		while (!CancellationToken.IsCancellationRequested) {
			var result = await CreateBackup();
			if (result.Kind.ShouldRetry()) {
				Logger.Warning("Scheduled backup failed, retrying in {Minutes} minutes.", BackupFailureRetryDelay.TotalMinutes);
				await Task.Delay(BackupFailureRetryDelay, CancellationToken);
			}
			else {
				Logger.Warning("Scheduling next backup in {Minutes} minutes.", BackupInterval.TotalMinutes);
				await Task.Delay(BackupInterval, CancellationToken);
				await WaitForOnlinePlayers();
			}
		}
	}

	private async Task<BackupCreationResult> CreateBackup() {
		return await backupManager.CreateBackup(loggerName, session, CancellationToken.None);
	}

	private async Task WaitForOnlinePlayers() {
		bool needsToLogOfflinePlayersMessage = true;
		
		session.AddOutputListener(ServerOutputListener, maxLinesToReadFromHistory: 0);
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
				
				Logger.Verbose("Waiting for server output before checking for online players again...");
				await serverOutputWhileWaitingForOnlinePlayers.WaitHandle.WaitOneAsync(CancellationToken);
			}
		} finally {
			session.RemoveOutputListener(ServerOutputListener);
		}
	}

	private void ServerOutputListener(object? sender, string line) {
		if (!serverOutputWhileWaitingForOnlinePlayers.IsSet) {
			serverOutputWhileWaitingForOnlinePlayers.Set();
			Logger.Verbose("Detected server output, signalling to check for online players again.");
		}
	}
}
