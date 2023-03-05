using Serilog;

namespace Phantom.Utils.Runtime; 

public abstract class CancellableBackgroundTask {
	private readonly CancellationTokenSource cancellationTokenSource = new ();

	protected ILogger Logger { get; }
	protected CancellationToken CancellationToken { get; }

	private readonly TaskManager taskManager;
	private readonly string taskName;
	
	protected CancellableBackgroundTask(ILogger logger, TaskManager taskManager, string taskName) {
		this.Logger = logger;
		this.CancellationToken = cancellationTokenSource.Token;
		
		this.taskManager = taskManager;
		this.taskName = taskName;
	}

	protected void Start() {
		taskManager.Run(taskName, Run);
	}

	private async Task Run() {
		Logger.Debug("Task started.");

		try {
			await RunTask();
		} catch (OperationCanceledException) {
			// Ignore.
		} catch (Exception e) {
			Logger.Fatal(e, "Caught exception in task.");
		} finally {
			cancellationTokenSource.Dispose();
			Dispose();
			Logger.Debug("Task stopped.");
		}
	}
	
	protected abstract Task RunTask();

	protected virtual void Dispose() {}

	public void Stop() {
		try {
			cancellationTokenSource.Cancel();
		} catch (ObjectDisposedException) {
			// Ignore.
		}
	}
}
