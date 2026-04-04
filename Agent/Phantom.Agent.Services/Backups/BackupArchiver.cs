using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Formats.Tar;
using Phantom.Agent.Services.Instances;
using Phantom.Common.Data.Backups;
using Phantom.Utils.IO;
using Phantom.Utils.Logging;
using Serilog;

namespace Phantom.Agent.Services.Backups;

sealed class BackupArchiver(
	string destinationBasePath,
	string temporaryBasePath,
	string loggerName,
	InstanceProperties instanceProperties,
	CancellationToken cancellationToken
) {
	private readonly ILogger logger = PhantomLogger.Create<BackupArchiver>(loggerName);
	
	private bool IsDirectorySkipped(ImmutableList<string> relativePath) {
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
	
	public async Task<string?> CreateBackup(BackupCreationResult.Builder resultBuilder) {
		string guid = instanceProperties.InstanceGuid.ToString();
		string currentDateTime = DateTime.Now.ToString("yyyyMMdd-HHmmss");
		string backupDirectoryPath = Path.Combine(destinationBasePath, guid);
		string backupFilePath = Path.Combine(backupDirectoryPath, currentDateTime + ".tar");
		
		if (File.Exists(backupFilePath)) {
			resultBuilder.Kind = BackupCreationResultKind.BackupFileAlreadyExists;
			logger.Warning("Skipping backup, file already exists: {FilePath}", backupFilePath);
			return null;
		}
		
		try {
			Directories.Create(backupDirectoryPath, Chmod.URWX_GRX);
		} catch (Exception e) {
			resultBuilder.Kind = BackupCreationResultKind.CouldNotCreateBackupDirectory;
			logger.Error(e, "Could not create backup directory: {DirectoryPath}", backupDirectoryPath);
			return null;
		}
		
		string temporaryDirectoryPath = Path.Combine(temporaryBasePath, guid + "_" + currentDateTime);
		if (!await CopyInstanceDirectoryAndCreateTarArchive(temporaryDirectoryPath, backupFilePath, resultBuilder)) {
			return null;
		}
		
		logger.Information("Created backup: {FilePath}", backupFilePath);
		return backupFilePath;
	}
	
	private async Task<bool> CopyInstanceDirectoryAndCreateTarArchive(string temporaryDirectoryPath, string backupFilePath, BackupCreationResult.Builder resultBuilder) {
		try {
			if (!await CopyInstanceDirectoryIntoTemporaryDirectory(temporaryDirectoryPath)) {
				resultBuilder.Kind = BackupCreationResultKind.CouldNotCopyInstanceIntoTemporaryDirectory;
				return false;
			}
			
			if (!await CreateTarArchive(temporaryDirectoryPath, backupFilePath)) {
				resultBuilder.Kind = BackupCreationResultKind.CouldNotCreateBackupArchive;
				return false;
			}
			
			return true;
		} finally {
			try {
				Directory.Delete(temporaryDirectoryPath, recursive: true);
			} catch (Exception e) {
				resultBuilder.Warnings |= BackupCreationWarnings.CouldNotDeleteTemporaryDirectory;
				logger.Error(e, "Could not delete temporary directory: {DirectoryPath}", temporaryDirectoryPath);
			}
		}
	}
	
	private async Task<bool> CopyInstanceDirectoryIntoTemporaryDirectory(string temporaryDirectoryPath) {
		try {
			await CopyDirectory(new DirectoryInfo(instanceProperties.InstanceDirectoryPath), temporaryDirectoryPath, ImmutableList<string>.Empty);
			return true;
		} catch (Exception e) {
			logger.Error(e, "Could not copy instance directory into temporary directory: {DirectoryPath}", temporaryDirectoryPath);
			return false;
		}
	}
	
	private async Task CopyDirectory(DirectoryInfo sourceDirectory, string destinationDirectoryPath, ImmutableList<string> relativePath) {
		cancellationToken.ThrowIfCancellationRequested();
		
		bool needsToCreateDirectory = true;
		
		foreach (FileInfo file in sourceDirectory.EnumerateFiles()) {
			var filePath = relativePath.Add(file.Name);
			if (IsFileSkipped(filePath)) {
				logger.Debug("Skipping file: {FilePath}", PathString(filePath));
				continue;
			}
			
			if (needsToCreateDirectory) {
				needsToCreateDirectory = false;
				Directories.Create(destinationDirectoryPath, Chmod.URWX);
			}
			
			await CopyFileWithRetries(file, Path.Combine(destinationDirectoryPath, file.Name));
		}
		
		foreach (DirectoryInfo directory in sourceDirectory.EnumerateDirectories()) {
			var directoryPath = relativePath.Add(directory.Name);
			if (IsDirectorySkipped(directoryPath)) {
				logger.Debug("Skipping directory: {DirectoryPath}", PathString(directoryPath));
				continue;
			}
			
			await CopyDirectory(directory, Path.Join(destinationDirectoryPath, directory.Name), directoryPath);
		}
		
		return;
		
		static string PathString(ImmutableList<string> filePath) {
			return string.Join(separator: '/', filePath);
		}
	}
	
	private async Task CopyFileWithRetries(FileInfo sourceFile, string destinationFilePath) {
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
					logger.Warning("Failed copying file {FilePath}, retrying...", sourceFile.FullName);
				}
			}
			
			await Task.Delay(millisecondsDelay: 200, cancellationToken);
		}
	}
	
	private async Task<bool> CreateTarArchive(string sourceDirectoryPath, string backupFilePath) {
		try {
			await TarFile.CreateFromDirectoryAsync(sourceDirectoryPath, backupFilePath, includeBaseDirectory: false, cancellationToken);
			return true;
		} catch (Exception e) {
			logger.Error(e, "Could not create archive.");
			DeleteBrokenArchiveFile(backupFilePath);
			return false;
		}
	}
	
	private void DeleteBrokenArchiveFile(string filePath) {
		if (File.Exists(filePath)) {
			try {
				File.Delete(filePath);
			} catch (Exception e) {
				logger.Error(e, "Could not delete broken archive: {FilePath}", filePath);
			}
		}
	}
}
