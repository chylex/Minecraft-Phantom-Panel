using Phantom.Agent.Minecraft.Instance;
using Phantom.Common.Data.Backups;
using Phantom.Common.Logging;
using Serilog;

namespace Phantom.Agent.Services.Backups;

sealed class BackupManager {
	private readonly string destinationBasePath;
	private readonly string temporaryBasePath;

	public BackupManager(AgentFolders agentFolders) {
		this.destinationBasePath = agentFolders.BackupsFolderPath;
		this.temporaryBasePath = Path.Combine(agentFolders.TemporaryFolderPath, "backups");
	}

	public async Task<BackupCreationResult> CreateBackup(string loggerName, InstanceProcess process, CancellationToken cancellationToken) {
		try {
			if (!await process.BackupSemaphore.Wait(TimeSpan.FromSeconds(1), cancellationToken)) {
				return new BackupCreationResult(BackupCreationResultKind.BackupAlreadyRunning);
			}
		} catch (ObjectDisposedException) {
			return new BackupCreationResult(BackupCreationResultKind.InstanceNotRunning);
		} catch (OperationCanceledException) {
			return new BackupCreationResult(BackupCreationResultKind.InstanceNotRunning);
		}

		try {
			return await new BackupCreator(destinationBasePath, temporaryBasePath, loggerName, process, cancellationToken).CreateBackup();
		} finally {
			process.BackupSemaphore.Release();
		}
	}

	private sealed class BackupCreator {
		private readonly string destinationBasePath;
		private readonly string temporaryBasePath;
		private readonly string loggerName;
		private readonly ILogger logger;
		private readonly InstanceProcess process;
		private readonly CancellationToken cancellationToken;

		public BackupCreator(string destinationBasePath, string temporaryBasePath, string loggerName, InstanceProcess process, CancellationToken cancellationToken) {
			this.destinationBasePath = destinationBasePath;
			this.temporaryBasePath = temporaryBasePath;
			this.loggerName = loggerName;
			this.logger = PhantomLogger.Create<BackupManager>(loggerName);
			this.process = process;
			this.cancellationToken = cancellationToken;
		}

		public async Task<BackupCreationResult> CreateBackup() {
			logger.Information("Backup started.");
			
			var resultBuilder = new BackupCreationResult.Builder();
			string? backupFilePath;
			
			using (var dispatcher = new BackupServerCommandDispatcher(logger, process, cancellationToken)) {
				backupFilePath = await CreateWorldArchive(dispatcher, resultBuilder);
			}
			
			if (backupFilePath != null) {
				await CompressWorldArchive(backupFilePath, resultBuilder);
			}
			
			var result = resultBuilder.Build();
			LogBackupResult(result);
			return result;
		}

		private async Task<string?> CreateWorldArchive(BackupServerCommandDispatcher dispatcher, BackupCreationResult.Builder resultBuilder) {
			try {
				await dispatcher.DisableAutomaticSaving();
				await dispatcher.SaveAllChunks();
				return await new BackupArchiver(destinationBasePath, temporaryBasePath, loggerName, process.InstanceProperties, cancellationToken).ArchiveWorld(resultBuilder);
			} catch (OperationCanceledException) {
				resultBuilder.Kind = BackupCreationResultKind.BackupCancelled;
				logger.Warning("Backup creation was cancelled.");
				return null;
			} catch (Exception e) {
				resultBuilder.Kind = BackupCreationResultKind.UnknownError;
				logger.Error(e, "Caught exception while creating an instance backup.");
				return null;
			} finally {
				try {
					await dispatcher.EnableAutomaticSaving();
				} catch (OperationCanceledException) {
					// ignore
				} catch (Exception e) {
					resultBuilder.Warnings |= BackupCreationWarnings.CouldNotRestoreAutomaticSaving;
					logger.Error(e, "Caught exception while enabling automatic saving after creating an instance backup.");
				}
			}
		}

		private async Task CompressWorldArchive(string filePath, BackupCreationResult.Builder resultBuilder) {
			var compressedFilePath = await BackupCompressor.Compress(filePath, cancellationToken);
			if (compressedFilePath == null) {
				resultBuilder.Warnings |= BackupCreationWarnings.CouldNotCompressWorldArchive;
			}
		}

		private void LogBackupResult(BackupCreationResult result) {
			if (result.Kind != BackupCreationResultKind.Success) {
				logger.Warning("Backup failed: {Reason}", result.Kind.ToSentence());
				return;
			}
			
			var warningCount = result.Warnings.Count();
			if (warningCount > 0) {
				logger.Warning("Backup finished with {Warnings} warning(s).", warningCount);
			}
			else {
				logger.Information("Backup finished successfully.");
			}
		}
	}
}
