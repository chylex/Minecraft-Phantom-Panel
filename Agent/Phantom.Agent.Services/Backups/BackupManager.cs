using Phantom.Agent.Services.Instances.State;
using Phantom.Common.Data.Backups;
using Phantom.Utils.Logging;
using Serilog;

namespace Phantom.Agent.Services.Backups;

sealed class BackupManager(AgentDirectories agentDirectories, int maxConcurrentCompressionTasks) : IDisposable {
	private readonly string destinationBasePath = agentDirectories.BackupsDirectoryPath;
	private readonly string temporaryBasePath = Path.Combine(agentDirectories.TemporaryDirectoryPath, "backups");
	private readonly SemaphoreSlim compressionSemaphore = new (maxConcurrentCompressionTasks, maxConcurrentCompressionTasks);
	
	public Task<BackupCreationResult> CreateBackup(string loggerName, InstanceProcess process, CancellationToken cancellationToken) {
		return new BackupCreator(this, loggerName, process, cancellationToken).CreateBackup();
	}
	
	public void Dispose() {
		compressionSemaphore.Dispose();
	}
	
	private sealed class BackupCreator(BackupManager manager, string loggerName, InstanceProcess process, CancellationToken cancellationToken) {
		private readonly ILogger logger = PhantomLogger.Create<BackupManager>(loggerName);
		
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
				return await new BackupArchiver(manager.destinationBasePath, manager.temporaryBasePath, loggerName, process.InstanceProperties, cancellationToken).CreateBackup(resultBuilder);
			} catch (OperationCanceledException) {
				resultBuilder.Kind = BackupCreationResultKind.BackupCancelled;
				logger.Warning("Backup creation was cancelled.");
				return null;
			} catch (TimeoutException) {
				resultBuilder.Kind = BackupCreationResultKind.BackupTimedOut;
				logger.Warning("Backup creation timed out.");
				return null;
			} catch (Exception e) {
				resultBuilder.Kind = BackupCreationResultKind.UnknownError;
				logger.Error(e, "Caught exception while creating an instance backup.");
				return null;
			} finally {
				try {
					await dispatcher.EnableAutomaticSaving();
				} catch (OperationCanceledException) {
					// Ignore.
				} catch (TimeoutException) {
					resultBuilder.Warnings |= BackupCreationWarnings.CouldNotRestoreAutomaticSaving;
					logger.Warning("Timed out waiting for automatic saving to be re-enabled.");
				} catch (Exception e) {
					resultBuilder.Warnings |= BackupCreationWarnings.CouldNotRestoreAutomaticSaving;
					logger.Error(e, "Caught exception while enabling automatic saving after creating an instance backup.");
				}
			}
		}
		
		private async Task CompressWorldArchive(string filePath, BackupCreationResult.Builder resultBuilder) {
			if (!await manager.compressionSemaphore.WaitAsync(TimeSpan.FromSeconds(1), cancellationToken)) {
				logger.Information("Too many compression tasks running, waiting for one of them to complete...");
				await manager.compressionSemaphore.WaitAsync(cancellationToken);
			}
			
			logger.Information("Compressing backup...");
			try {
				var compressedFilePath = await BackupCompressor.Compress(filePath, cancellationToken);
				if (compressedFilePath == null) {
					resultBuilder.Warnings |= BackupCreationWarnings.CouldNotCompressBackupArchive;
				}
			} finally {
				manager.compressionSemaphore.Release();
			}
		}
		
		private void LogBackupResult(BackupCreationResult result) {
			if (result.Kind != BackupCreationResultKind.Success) {
				logger.Warning("Backup failed: {Reason}", DescribeResult(result.Kind));
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
		
		private static string DescribeResult(BackupCreationResultKind kind) {
			return kind switch {
				BackupCreationResultKind.Success                                    => "Backup created successfully.",
				BackupCreationResultKind.InstanceNotRunning                         => "Instance is not running.",
				BackupCreationResultKind.BackupCancelled                            => "Backup cancelled.",
				BackupCreationResultKind.BackupTimedOut                             => "Backup timed out.",
				BackupCreationResultKind.BackupAlreadyRunning                       => "A backup is already being created.",
				BackupCreationResultKind.BackupFileAlreadyExists                    => "Backup with the same name already exists.",
				BackupCreationResultKind.CouldNotCreateBackupDirectory              => "Could not create backup directory.",
				BackupCreationResultKind.CouldNotCopyInstanceIntoTemporaryDirectory => "Could not copy instance into temporary directory.",
				BackupCreationResultKind.CouldNotCreateBackupArchive                => "Could not create backup archive.",
				_                                                                   => "Unknown error.",
			};
		}
	}
}
