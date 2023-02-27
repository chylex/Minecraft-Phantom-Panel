using System.Diagnostics;
using Serilog;

namespace Phantom.Utils.Runtime;

public sealed class OneShotProcess {
	private readonly ILogger logger;
	private readonly ProcessStartInfo startInfo;
	
	public event DataReceivedEventHandler? Output;
	
	public OneShotProcess(ILogger logger, ProcessStartInfo startInfo) {
		this.logger = logger;
		this.startInfo = startInfo;
		this.startInfo.RedirectStandardOutput = true;
		this.startInfo.RedirectStandardError = true;
	}

	public async Task<bool> Run(CancellationToken cancellationToken) {
		using var process = new Process { StartInfo = startInfo };
		process.OutputDataReceived += Output;
		process.ErrorDataReceived += Output;

		try {
			process.Start();
			process.BeginOutputReadLine();
			process.BeginErrorReadLine();
		} catch (Exception e) {
			logger.Error(e, "Caught exception launching process.");
			return false;
		}

		try {
			await process.WaitForExitAsync(cancellationToken);
		} catch (OperationCanceledException) {
			await TryKillProcess(process);
			return false;
		} catch (Exception e) {
			logger.Error(e, "Caught exception waiting for process to exit.");
			return false;
		}

		if (!process.HasExited) {
			await TryKillProcess(process);
			return false;
		}

		if (process.ExitCode != 0) {
			logger.Error("Process exited with code {ExitCode}.", process.ExitCode);
			return false;
		}

		logger.Debug("Process finished successfully.");
		return true;
	}

	private async Task TryKillProcess(Process process) {
		using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));

		try {
			process.Kill();
			await process.WaitForExitAsync(timeout.Token);
		} catch (OperationCanceledException) {
			logger.Error("Timed out waiting for killed process to exit.");
		} catch (Exception e) {
			logger.Error(e, "Caught exception killing process.");
		}
	}
}
