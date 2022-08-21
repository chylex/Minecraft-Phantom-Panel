using Phantom.Agent.Minecraft.Instance;
using Phantom.Agent.Minecraft.Launcher;
using Phantom.Common.Data;
using Phantom.Common.Logging;
using Serilog;

namespace Phantom.Agent.Services.Instances;

sealed class Instance : IDisposable {
	public InstanceInfo Info { get; }

	private readonly BaseLauncher launcher;
	private InstanceSession? currentSession;

	private readonly ILogger logger;
	private readonly SemaphoreSlim semaphore = new (1, 1);
	private bool isStopping;

	public Instance(InstanceInfo info, BaseLauncher launcher) {
		this.logger = PhantomLogger.Create<Instance>(info.InstanceGuid.ToString());

		this.Info = info;
		this.launcher = launcher;
	}

	public async Task<bool> Launch(CancellationToken cancellationToken) {
		await semaphore.WaitAsync(cancellationToken);
		try {
			if (currentSession != null) {
				return false;
			}

			logger.Information("Session starting...");
			currentSession = await launcher.Launch();
			currentSession.AddOutputListener(CurrentSessionOutput);
			currentSession.SessionEnded += CurrentSessionEnded;

			if (currentSession.HasEnded) {
				logger.Warning("Session ended immediately after it was started.");
				currentSession.Dispose();
				currentSession = null;
				return false;
			}
			else {
				logger.Information("Session started.");
				return true;
			}
		} finally {
			semaphore.Release();
		}
	}

	private void CurrentSessionOutput(object? sender, string e) {
		logger.Verbose("[Server] {Line}", e);
	}

	private void CurrentSessionEnded(object? sender, EventArgs e) {
		try {
			semaphore.Wait();
			try {
				if (currentSession == sender && currentSession != null) {
					currentSession.Dispose();
					currentSession = null;
					logger.Information("Session ended.");
				}
			} finally {
				semaphore.Release();
			}
		} catch (OperationCanceledException) {
			logger.Warning("Semaphore was cancelled while handling session ended event.");
		}
	}

	public async Task Stop(CancellationToken cancellationToken) {
		try {
			await semaphore.WaitAsync(cancellationToken);

			try {
				if (currentSession != null) {
					logger.Information("Stopping session...");
					await currentSession.SendCommand("stop", cancellationToken);
				}
			} finally {
				semaphore.Release();
			}
		} catch (OperationCanceledException) {
			logger.Warning("Semaphore was cancelled while stopping session.");
		}
	}

	public async Task StopAndWaitForExit(TimeSpan waitTime) {
		var cts = new CancellationTokenSource(waitTime);
		var token = cts.Token;

		try {
			InstanceSession? session = null;
			try {
				isStopping = true;
				await semaphore.WaitAsync(token);

				try {
					session = currentSession;
					if (session == null) {
						return;
					}

					logger.Information("Stopping session...");
					await session.SendCommand("stop", token);
				} finally {
					semaphore.Release();
				}
			} catch (OperationCanceledException) {
				logger.Warning("Semaphore was cancelled while stopping session and waiting for exit.");
			}

			if (session != null) {
				try {
					logger.Information("Waiting for session to end...");
					await session.WaitForExit(token);
				} catch (OperationCanceledException) {
					try {
						logger.Warning("Waiting timed out, killing session...");
						session.Kill();
					} catch (Exception e) {
						logger.Warning(e, "Caught exception while killing session.");
					}
				}
			}
		} finally {
			cts.Dispose();
		}
	}

	public void Dispose() {
		currentSession?.Dispose();
		semaphore.Dispose();
	}
}
