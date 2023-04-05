using Serilog;

namespace Phantom.Utils.Processes;

public sealed class OneShotProcess {
	private readonly ILogger logger;
	private readonly ProcessConfigurator configurator;
	
	public event EventHandler<Process.Output>? OutputReceived;
	
	public OneShotProcess(ILogger logger, ProcessConfigurator configurator) {
		this.logger = logger;
		this.configurator = configurator;
	}

	public async Task<bool> Run(CancellationToken cancellationToken) {
		using var process = configurator.CreateProcess();
		process.OutputReceived += OutputReceived;

		try {
			process.Start();
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
