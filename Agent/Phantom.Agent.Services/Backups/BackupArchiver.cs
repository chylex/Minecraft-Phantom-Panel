using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Formats.Tar;
using Phantom.Agent.Minecraft.Instance;
using Phantom.Common.Data.Backups;
using Phantom.Common.Logging;
using Phantom.Utils.IO;
using Serilog;

namespace Phantom.Agent.Services.Backups; 

sealed class BackupArchiver {
	private readonly ILogger logger;
	private readonly InstanceProperties instanceProperties;
	private readonly CancellationToken cancellationToken;
	
	public BackupArchiver(string loggerName, InstanceProperties instanceProperties, CancellationToken cancellationToken) {
		this.logger = PhantomLogger.Create<BackupArchiver>(loggerName);
		this.instanceProperties = instanceProperties;
		this.cancellationToken = cancellationToken;
	}

	private bool IsFolderSkipped(ImmutableList<string> relativePath) {
		return relativePath is ["cache" or "crash-reports" or "debug" or "libraries" or "logs" or "mods" or "versions"];
	}
	
	[SuppressMessage("ReSharper", "ConvertIfStatementToReturnStatement")]
	private bool IsFileSkipped(ImmutableList<string> relativePath) {
		var name = relativePath[^1];
		
		if (relativePath.Count == 2 && name == "session.lock") {
			return true;
		}

		var extension = Path.GetExtension(name);
		if (extension is ".jar" or ".zip") {
			return true;
		}
		
		return false;
	}

	public async Task ArchiveWorld(string destinationPath, BackupCreationResult.Builder resultBuilder) {
		string backupFolderPath = Path.Combine(destinationPath, instanceProperties.InstanceGuid.ToString());
		string backupFilePath = Path.Combine(backupFolderPath, DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".tar");
		
		if (File.Exists(backupFilePath)) {
			resultBuilder.Kind = BackupCreationResultKind.BackupAlreadyExists;
			logger.Warning("Skipping backup, file already exists: {File}", backupFilePath);
			return;
		}
		
		try {
			Directories.Create(backupFolderPath, Chmod.URWX_GRX);
		} catch (Exception e) {
			resultBuilder.Kind = BackupCreationResultKind.CouldNotCreateBackupFolder;
			logger.Error(e, "Could not create backup folder: {Folder}", backupFolderPath);
			return;
		}

		if (!await CopyWorldAndCreateTarArchive(backupFolderPath, backupFilePath, resultBuilder)) {
			return;
		}
		
		var compressedFilePath = await BackupCompressor.Compress(backupFilePath, cancellationToken);
		if (compressedFilePath == null) {
			resultBuilder.Warnings |= BackupCreationWarnings.CouldNotCompressWorldArchive;
		}
	}

	private async Task<bool> CopyWorldAndCreateTarArchive(string backupFolderPath, string backupFilePath, BackupCreationResult.Builder resultBuilder) {
		string temporaryFolderPath = Path.Combine(backupFolderPath, "temp");
		
		try {
			if (!await CopyWorldToTemporaryFolder(temporaryFolderPath)) {
				resultBuilder.Kind = BackupCreationResultKind.CouldNotCopyWorldToTemporaryFolder;
				return false;
			}

			if (!await CreateTarArchive(temporaryFolderPath, backupFilePath)) {
				resultBuilder.Kind = BackupCreationResultKind.CouldNotCreateWorldArchive;
				return false;
			}

			return true;
		} finally {
			try {
				Directory.Delete(temporaryFolderPath, recursive: true);
			} catch (Exception e) {
				resultBuilder.Warnings |= BackupCreationWarnings.CouldNotDeleteTemporaryFolder;
				logger.Error(e, "Could not delete temporary world folder: {Folder}", temporaryFolderPath);
			}
		}
	}

	private async Task<bool> CopyWorldToTemporaryFolder(string temporaryFolderPath) {
		try {
			await CopyDirectory(new DirectoryInfo(instanceProperties.InstanceFolder), temporaryFolderPath, ImmutableList<string>.Empty);
			return true;
		} catch (Exception e) {
			logger.Error(e, "Could not copy world to temporary folder.");
			return false;
		}
	}

	private async Task<bool> CreateTarArchive(string sourceFolderPath, string backupFilePath) {
		try {
			await TarFile.CreateFromDirectoryAsync(sourceFolderPath, backupFilePath, false, cancellationToken);
			return true;
		} catch (Exception e) {
			logger.Error(e, "Could not create archive.");
			return false;
		}
	}

	private async Task CopyDirectory(DirectoryInfo sourceFolder, string destinationFolderPath, ImmutableList<string> relativePath) {
		cancellationToken.ThrowIfCancellationRequested();

		bool needsToCreateFolder = true;
		
		foreach (FileInfo file in sourceFolder.EnumerateFiles()) {
			var filePath = relativePath.Add(file.Name);
			if (IsFileSkipped(filePath)) {
				logger.Verbose("Skipping file: {File}", string.Join('/', filePath));
				continue;
			}

			if (needsToCreateFolder) {
				needsToCreateFolder = false;
				Directories.Create(destinationFolderPath, Chmod.URWX);
			}

			await CopyFileWithRetries(file, destinationFolderPath);
		}

		foreach (DirectoryInfo directory in sourceFolder.EnumerateDirectories()) {
			var folderPath = relativePath.Add(directory.Name);
			if (IsFolderSkipped(folderPath)) {
				logger.Verbose("Skipping folder: {Folder}", string.Join('/', folderPath));
				continue;
			}
			
			await CopyDirectory(directory, Path.Join(destinationFolderPath, directory.Name), folderPath);
		}
	}

	private async Task CopyFileWithRetries(FileInfo sourceFile, string destinationFolderPath) {
		var destinationFilePath = Path.Combine(destinationFolderPath, sourceFile.Name);
		
		const int TotalAttempts = 10;
		for (int attempt = 1; attempt <= TotalAttempts; attempt++) {
			try {
				sourceFile.CopyTo(destinationFilePath);
				return;
			} catch (IOException) {
				if (attempt == TotalAttempts) {
					throw;
				}
				else {
					logger.Warning("Failed copying file {File}, retrying...", sourceFile.FullName);
					await Task.Delay(200, cancellationToken);
				}
			}
		}
	}
}
