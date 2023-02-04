using Serilog;

namespace Phantom.Utils.Runtime; 

public abstract class CancellableBackgroundTask {
	private readonly CancellationTokenSource cancellationTokenSource = new ();

	protected ILogger Logger { get; }
	protected CancellationToken CancellationToken { get; }

	protected CancellableBackgroundTask(ILogger logger, TaskManager taskManager, string taskName) {
		this.Logger = logger;
		this.CancellationToken = cancellationTokenSource.Token;
		taskManager.Run(taskName, Run);
	}

	private async Task Run() {
		Logger.Verbose("Task started.");

		try {
			await RunTask();
		} catch (OperationCanceledException) {
			// Ignore.
		} catch (Exception e) {
			Logger.Fatal(e, "Caught exception in task.");
		} finally {
			cancellationTokenSource.Dispose();
			Logger.Verbose("Task stopped.");
		}
	}
	
	protected abstract Task RunTask();

	public void Stop() {
		try {
			cancellationTokenSource.Cancel();
		} catch (ObjectDisposedException) {
			// Ignore.
		}
	}
}
