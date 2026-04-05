using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Formats.Tar;
using System.Security.Cryptography;
using Phantom.Agent.Services.Instances;
using Phantom.Common.Data.Backups;
using Phantom.Utils.IO;
using Phantom.Utils.Logging;
using Serilog;

namespace Phantom.Agent.Services.Backups;

static class BackupArchiver {
	public static async Task<string?> Run(string loggerName, string destinationBasePath, string temporaryBasePath, InstanceProperties instanceProperties, BackupCreationResult.Builder resuiltBuilder, CancellationToken cancellationToken) {
		string guid = instanceProperties.InstanceGuid.ToString();
		string currentDateTime = DateTime.Now.ToString("yyyyMMdd-HHmmss");
		
		string backupDirectoryPath = Path.Combine(destinationBasePath, guid);
		string backupFilePath = Path.Combine(backupDirectoryPath, currentDateTime + ".tar");
		string temporaryDirectoryPath = Path.Combine(temporaryBasePath, guid + "_" + currentDateTime);
		
		return await new Runner(loggerName, backupDirectoryPath, backupFilePath, temporaryDirectoryPath, instanceProperties, resuiltBuilder, cancellationToken).Run();
	}
	
	private static bool IsDirectorySkipped(RelativePath relativePath) {
		return relativePath.Components is ["cache" or "crash-reports" or "debug" or "libraries" or "logs" or "mods" or "versions"];
	}
	
	[SuppressMessage("ReSharper", "ConvertIfStatementToReturnStatement")]
	private static bool IsFileSkipped(RelativePath relativePath) {
		var name = relativePath.Components[^1];
		
		if (relativePath.Components.Count == 2 && name == "session.lock") {
			return true;
		}
		
		var extension = Path.GetExtension(name);
		if (extension is ".jar" or ".zip") {
			return true;
		}
		
		return false;
	}
	
	private sealed class Runner(
		string loggerName,
		string backupDirectoryPath,
		string backupFilePath,
		string temporaryDirectoryPath,
		InstanceProperties instanceProperties,
		BackupCreationResult.Builder resultBuilder,
		CancellationToken cancellationToken
	) {
		private readonly ILogger logger = PhantomLogger.Create(nameof(BackupArchiver), loggerName);
		private readonly FileHashComparer fileHashComparer = new (cancellationToken);
		
		public async Task<string?> Run() {
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
			
			try {
				if (!await CopyInstanceDirectoryIntoTemporaryDirectory()) {
					resultBuilder.Kind = BackupCreationResultKind.CouldNotCopyInstanceIntoTemporaryDirectory;
					return null;
				}
				
				if (!await CreateBackupArchiveFromTemporaryDirectory()) {
					resultBuilder.Kind = BackupCreationResultKind.CouldNotCreateBackupArchive;
					return null;
				}
				
				logger.Information("Created backup: {FilePath}", backupFilePath);
				return backupFilePath;
			} finally {
				try {
					Directory.Delete(temporaryDirectoryPath, recursive: true);
				} catch (Exception e1) {
					resultBuilder.Warnings |= BackupCreationWarnings.CouldNotDeleteTemporaryDirectory;
					logger.Error(e1, "Could not delete temporary directory: {DirectoryPath}", temporaryDirectoryPath);
				}
			}
		}
		
		private async Task<bool> CopyInstanceDirectoryIntoTemporaryDirectory() {
			try {
				await CopyDirectory(new DirectoryInfo(instanceProperties.InstanceDirectoryPath), RelativePath.Empty, temporaryDirectoryPath);
				return true;
			} catch (Exception e) {
				logger.Error(e, "Could not copy instance directory into temporary directory: {DirectoryPath}", temporaryDirectoryPath);
				return false;
			}
		}
		
		private async Task CopyDirectory(DirectoryInfo sourceDirectory, RelativePath sourceDirectoryRelativePath, string destinationDirectoryPath) {
			cancellationToken.ThrowIfCancellationRequested();
			
			bool needsToCreateDirectory = true;
			
			foreach (FileInfo file in sourceDirectory.EnumerateFiles()) {
				var filePath = sourceDirectoryRelativePath.Child(file.Name);
				if (IsFileSkipped(filePath)) {
					logger.Verbose("Skipped file: {FilePath}", filePath);
					continue;
				}
				
				if (needsToCreateDirectory) {
					needsToCreateDirectory = false;
					Directories.Create(destinationDirectoryPath, Chmod.URWX);
				}
				
				await CopyFileWithRetries(file, filePath, Path.Combine(destinationDirectoryPath, file.Name));
				logger.Verbose("Copied file: {FilePath}", filePath);
			}
			
			foreach (DirectoryInfo directory in sourceDirectory.EnumerateDirectories()) {
				var directoryPath = sourceDirectoryRelativePath.Child(directory.Name);
				if (IsDirectorySkipped(directoryPath)) {
					logger.Verbose("Skipped directory: {DirectoryPath}", directoryPath);
					continue;
				}
				
				await CopyDirectory(directory, directoryPath, Path.Join(destinationDirectoryPath, directory.Name));
			}
		}
		
		private async Task CopyFileWithRetries(FileInfo sourceFile, RelativePath sourceFileRelativePath, string destinationFilePath) {
			const int TotalAttempts = 10;
			
			for (int attempt = 1; attempt <= TotalAttempts; attempt++) {
				try {
					FileInfo destinationFile = sourceFile.CopyTo(destinationFilePath, overwrite: true);
					
					if (await fileHashComparer.CheckHashEquals(sourceFile, destinationFile)) {
						return;
					}
					
					if (attempt == TotalAttempts) {
						logger.Warning("File {FilePath} changed while copying, using last attempt", sourceFileRelativePath);
						resultBuilder.Warnings |= BackupCreationWarnings.SomeFilesChangedDuringBackup;
					}
					else {
						logger.Warning("File {FilePath} changed while copying, retrying...", sourceFileRelativePath);
					}
				} catch (IOException) {
					if (attempt == TotalAttempts) {
						throw;
					}
					else {
						logger.Warning("Failed copying file {FilePath}, retrying...", sourceFileRelativePath);
					}
				}
				
				await Task.Delay(millisecondsDelay: 200, cancellationToken);
			}
		}
		
		private async Task<bool> CreateBackupArchiveFromTemporaryDirectory() {
			try {
				await TarFile.CreateFromDirectoryAsync(temporaryDirectoryPath, backupFilePath, includeBaseDirectory: false, cancellationToken);
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
	
	private readonly record struct RelativePath(ImmutableList<string> Components) {
		public static RelativePath Empty => new (ImmutableList<string>.Empty);
		
		public RelativePath Child(string component) {
			return new RelativePath(Components.Add(component));
		}
		
		public override string ToString() {
			return string.Join(separator: '/', Components);
		}
	}
	
	private sealed class FileHashComparer(CancellationToken cancellationToken) {
		private static readonly FileStreamOptions FileStreamOptions = new () {
			Mode = FileMode.Open,
			Access = FileAccess.Read,
			Share = FileShare.ReadWrite,
			Options = FileOptions.Asynchronous | FileOptions.SequentialScan,
			BufferSize = 65536,
		};
		
		private readonly Memory<byte> hash1 = new byte[SHA1.HashSizeInBytes];
		private readonly Memory<byte> hash2 = new byte[SHA1.HashSizeInBytes];
		
		public async Task<bool> CheckHashEquals(FileInfo file1, FileInfo file2) {
			await ComputeHash(file1, hash1);
			await ComputeHash(file2, hash2);
			return hash1.Span.SequenceEqual(hash2.Span);
		}
		
		private async Task ComputeHash(FileInfo file, Memory<byte> destination) {
			await using var fileStream = file.Open(FileStreamOptions);
			await SHA1.HashDataAsync(fileStream, destination, cancellationToken);
		}
	}
}
