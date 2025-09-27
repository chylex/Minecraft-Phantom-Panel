using Phantom.Agent.Minecraft.Instance;
using Phantom.Common.Data.Backups;
using Phantom.Utils.Logging;
using Serilog;

namespace Phantom.Agent.Services.Backups;

sealed class BackupManager : IDisposable {
	private readonly string destinationBasePath;
	private readonly string temporaryBasePath;
	private readonly SemaphoreSlim compressionSemaphore;
	
	public BackupManager(AgentFolders agentFolders, int maxConcurrentCompressionTasks) {
		this.destinationBasePath = agentFolders.BackupsFolderPath;
		this.temporaryBasePath = Path.Combine(agentFolders.TemporaryFolderPath, "backups");
		this.compressionSemaphore = new SemaphoreSlim(maxConcurrentCompressionTasks, maxConcurrentCompressionTasks);
	}
	
	public Task<BackupCreationResult> CreateBackup(string loggerName, InstanceProcess process, CancellationToken cancellationToken) {
		return new BackupCreator(this, loggerName, process, cancellationToken).CreateBackup();
	}
	
	public void Dispose() {
		compressionSemaphore.Dispose();
	}
	
	private sealed class BackupCreator {
		private readonly BackupManager manager;
		private readonly string loggerName;
		private readonly ILogger logger;
		private readonly InstanceProcess process;
		private readonly CancellationToken cancellationToken;
		
		public BackupCreator(BackupManager manager, string loggerName, InstanceProcess process, CancellationToken cancellationToken) {
			this.manager = manager;
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
				return await new BackupArchiver(manager.destinationBasePath, manager.temporaryBasePath, loggerName, process.InstanceProperties, cancellationToken).ArchiveWorld(resultBuilder);
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
					resultBuilder.Warnings |= BackupCreationWarnings.CouldNotCompressWorldArchive;
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
				BackupCreationResultKind.Success                            => "Backup created successfully.",
				BackupCreationResultKind.InstanceNotRunning                 => "Instance is not running.",
				BackupCreationResultKind.BackupCancelled                    => "Backup cancelled.",
				BackupCreationResultKind.BackupTimedOut                     => "Backup timed out.",
				BackupCreationResultKind.BackupAlreadyRunning               => "A backup is already being created.",
				BackupCreationResultKind.BackupFileAlreadyExists            => "Backup with the same name already exists.",
				BackupCreationResultKind.CouldNotCreateBackupFolder         => "Could not create backup folder.",
				BackupCreationResultKind.CouldNotCopyWorldToTemporaryFolder => "Could not copy world to temporary folder.",
				BackupCreationResultKind.CouldNotCreateWorldArchive         => "Could not create world archive.",
				_                                                           => "Unknown error.",
			};
		}
	}
}
