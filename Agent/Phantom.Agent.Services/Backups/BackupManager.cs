using System.Text.RegularExpressions;
using Phantom.Agent.Minecraft.Command;
using Phantom.Agent.Minecraft.Instance;
using Phantom.Common.Data.Backups;
using Phantom.Common.Logging;
using Serilog;

namespace Phantom.Agent.Services.Backups;

sealed partial class BackupManager {
	private readonly string basePath;

	public BackupManager(AgentFolders agentFolders) {
		this.basePath = agentFolders.BackupsFolderPath;
	}

	public async Task<BackupCreationResult> CreateBackup(string loggerName, InstanceSession session, CancellationToken cancellationToken) {
		return await new BackupCreator(basePath, loggerName, session, cancellationToken).CreateBackup();
	}

	private sealed class BackupCreator {
		private readonly string basePath;
		private readonly string loggerName;
		private readonly ILogger logger;
		private readonly InstanceSession session;
		private readonly BackupCommandListener listener;
		private readonly CancellationToken cancellationToken;

		public BackupCreator(string basePath, string loggerName, InstanceSession session, CancellationToken cancellationToken) {
			this.basePath = basePath;
			this.loggerName = loggerName;
			this.logger = PhantomLogger.Create<BackupManager>(loggerName);
			this.session = session;
			this.listener = new BackupCommandListener(logger);
			this.cancellationToken = cancellationToken;
		}

		public async Task<BackupCreationResult> CreateBackup() {
			session.AddOutputListener(listener.OnOutput, 0);
			try {
				var resultBuilder = new BackupCreationResult.Builder();
				await RunBackupProcess(resultBuilder);
				return resultBuilder.Build();
			} finally {
				session.RemoveOutputListener(listener.OnOutput);
			}
		}
		
		private async Task RunBackupProcess(BackupCreationResult.Builder resultBuilder) {
			try {
				await DisableAutomaticSaving();
				await SaveAllChunks();
				await new BackupArchiver(loggerName, session.InstanceProperties, cancellationToken).ArchiveWorld(basePath, resultBuilder);
			} catch (OperationCanceledException) {
				resultBuilder.Kind = BackupCreationResultKind.BackupCancelled;
				logger.Warning("Backup creation was cancelled.");
			} catch (Exception e) {
				resultBuilder.Kind = BackupCreationResultKind.UnknownError;
				logger.Error(e, "Caught exception while creating an instance backup.");
			} finally {
				try {
					await EnableAutomaticSaving();
				} catch (OperationCanceledException) {
					// ignore
				} catch (Exception e) {
					resultBuilder.Warnings |= BackupCreationWarnings.CouldNotRestoreAutomaticSaving;
					logger.Error(e, "Caught exception while enabling automatic saving after creating an instance backup.");
				}
			}
		}

		private async Task DisableAutomaticSaving() {
			await session.SendCommand(MinecraftCommand.SaveOff, cancellationToken);
			await listener.AutomaticSavingDisabled.Task.WaitAsync(cancellationToken);
		}

		private async Task SaveAllChunks() {
			// TODO Try if not flushing and waiting a few seconds before flushing reduces lag.
			await session.SendCommand(MinecraftCommand.SaveAll(flush: true), cancellationToken);
			await listener.SavedTheGame.Task.WaitAsync(cancellationToken);
		}

		private async Task EnableAutomaticSaving() {
			await session.SendCommand(MinecraftCommand.SaveOn, cancellationToken);
			await listener.AutomaticSavingEnabled.Task.WaitAsync(cancellationToken);
		}
	}
	
	private sealed partial class BackupCommandListener {
		[GeneratedRegex(@"^\[(?:.*?)\] \[Server thread/INFO\]: (.*?)$", RegexOptions.NonBacktracking)]
		private static partial Regex ServerThreadInfoRegex();

		private readonly ILogger logger;

		public BackupCommandListener(ILogger logger) {
			this.logger = logger;
		}

		public TaskCompletionSource AutomaticSavingDisabled { get; } = new ();
		public TaskCompletionSource SavedTheGame { get; } = new ();
		public TaskCompletionSource AutomaticSavingEnabled { get; } = new ();

		public void OnOutput(object? sender, string? line) {
			if (line == null) {
				return;
			}

			var match = ServerThreadInfoRegex().Match(line);
			if (!match.Success) {
				return;
			}

			string info = match.Groups[1].Value;

			if (!AutomaticSavingDisabled.Task.IsCompleted) {
				if (info == "Automatic saving is now disabled") {
					logger.Verbose("Detected that automatic saving is disabled.");
					AutomaticSavingDisabled.SetResult();
				}
			}
			else if (!SavedTheGame.Task.IsCompleted) {
				if (info == "Saved the game") {
					logger.Verbose("Detected that the game is saved.");
					SavedTheGame.SetResult();
				}
			}
			else if (!AutomaticSavingEnabled.Task.IsCompleted) {
				if (info == "Automatic saving is now enabled") {
					logger.Verbose("Detected that automatic saving is enabled.");
					AutomaticSavingEnabled.SetResult();
				}
			}
		}
	}
}
