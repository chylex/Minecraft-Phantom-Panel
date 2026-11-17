using System.IO.Compression;
using Phantom.Utils.IO;
using Phantom.Utils.Logging;
using Serilog;

namespace Phantom.Agent.Services.Backups;

static class BackupCompressor {
	private static ILogger Logger { get; } = PhantomLogger.Create(nameof(BackupCompressor));
	
	public static async Task<string?> Compress(string sourceFilePath, CancellationToken cancellationToken) {
		if (sourceFilePath.Contains('"')) {
			Logger.Error("Could not compress backup, archive path contains quotes: {Path}", sourceFilePath);
			return null;
		}
		
		var destinationFilePath = sourceFilePath + ".zst";
		
		if (!await TryCompressFile(sourceFilePath, destinationFilePath, cancellationToken)) {
			try {
				File.Delete(destinationFilePath);
			} catch (Exception e) {
				Logger.Error(e, "Could not delete compresed archive after unsuccessful compression: {Path}", destinationFilePath);
			}
			
			return null;
		}
		
		return destinationFilePath;
	}
	
	private static async Task<bool> TryCompressFile(string sourceFilePath, string destinationFilePath, CancellationToken cancellationToken) {
		var workingDirectory = Path.GetDirectoryName(sourceFilePath);
		if (string.IsNullOrEmpty(workingDirectory)) {
			Logger.Error("Invalid destination path: {Path}", destinationFilePath);
			return false;
		}
		
		try {
			await using var sourceFileStream = File.OpenRead(sourceFilePath);
			await using var destinationFileStream = Files.OpenForWriting(destinationFilePath, FileMode.Create, Chmod.URW_GR);
			
			await using var zstdStream = new ZstandardStream(destinationFileStream, new ZstandardCompressionOptions {
				Quality = 10,
				WindowLog2 = 26,
			});
			
			await sourceFileStream.CopyToAsync(zstdStream, cancellationToken);
			return true;
		} catch (Exception e) {
			Logger.Error(e, "Could not compress backup");
			return false;
		}
	}
}
