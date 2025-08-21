using Phantom.Utils.Logging;
using Phantom.Utils.Processes;
using Serilog;

namespace Phantom.Agent.Services.Backups;

static class BackupCompressor {
	private static ILogger Logger { get; } = PhantomLogger.Create(nameof(BackupCompressor));
	private static ILogger ZstdLogger { get; } = PhantomLogger.Create(nameof(BackupCompressor), "Zstd");
	
	private const string Quality = "-10";
	private const string Memory = "--long=26";
	private const string Threads = "-T3";
	
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
		
		var launcher = new ProcessConfigurator {
			FileName = "zstd",
			WorkingDirectory = workingDirectory,
			ArgumentList = {
				Quality,
				Memory,
				Threads,
				"--rm",
				"--no-progress",
				"-o", destinationFilePath,
				"--", sourceFilePath
			}
		};
		
		static void OnZstdOutput(object? sender, Process.Output output) {
			if (!string.IsNullOrWhiteSpace(output.Line)) {
				ZstdLogger.Debug("[Output] {Line}", output.Line);
			}
		}
		
		var process = new OneShotProcess(ZstdLogger, launcher);
		process.OutputReceived += OnZstdOutput;
		return await process.Run(cancellationToken);
	}
}
