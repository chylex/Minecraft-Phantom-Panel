using System.Diagnostics;
using Phantom.Common.Logging;
using Serilog;

namespace Phantom.Agent.Services.Backups; 

static class BackupCompressor {
	private static ILogger Logger { get; } = PhantomLogger.Create(nameof(BackupCompressor));
	
	private const int Quality = 10;
	private const int Memory = 26;
	private const int Threads = 3;
	
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
		
		var startInfo = new ProcessStartInfo {
			FileName = "zstd",
			WorkingDirectory = workingDirectory,
			Arguments = $"-{Quality} --long={Memory} -T{Threads} -c --rm --no-progress -c -o \"{destinationFilePath}\" -- \"{sourceFilePath}\"",
			RedirectStandardOutput = true,
			RedirectStandardError = true
		};

		using var process = new Process { StartInfo = startInfo };
		process.OutputDataReceived += OnZstdProcessOutput;
		process.ErrorDataReceived += OnZstdProcessOutput;

		try {
			process.Start();
			process.BeginOutputReadLine();
			process.BeginErrorReadLine();
		} catch (Exception e) {
			Logger.Error(e, "Caught exception launching zstd process.");
		}

		try {
			await process.WaitForExitAsync(cancellationToken);
		} catch (OperationCanceledException) {
			await TryKillProcess(process);
			return false;
		} catch (Exception e) {
			Logger.Error(e, "Caught exception waiting for zstd process to exit.");
			return false;
		}

		if (!process.HasExited) {
			await TryKillProcess(process);
			return false;
		}

		if (process.ExitCode != 0) {
			Logger.Error("Zstd process exited with code {ExitCode}.", process.ExitCode);
			return false;
		}

		return true;
	}

	private static void OnZstdProcessOutput(object sender, DataReceivedEventArgs e) {
		if (!string.IsNullOrWhiteSpace(e.Data)) {
			Logger.Verbose("[Zstd] {Line}", e.Data);
		}
	}

	private static async Task TryKillProcess(Process process) {
		CancellationTokenSource timeout = new CancellationTokenSource(TimeSpan.FromSeconds(1));

		try {
			process.Kill();
			await process.WaitForExitAsync(timeout.Token);
		} catch (OperationCanceledException) {
			Logger.Error("Timed out waiting for killed zstd process to exit.");
		} catch (Exception e) {
			Logger.Error(e, "Caught exception killing zstd process.");
		}
	}
}
