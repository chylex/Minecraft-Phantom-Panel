using Serilog;

namespace Phantom.Utils.Tasks;

public abstract class CancellableBackgroundTask {
	private readonly CancellationTokenSource cancellationTokenSource = new ();
	
	protected ILogger Logger { get; }
	protected CancellationToken CancellationToken { get; }
	
	protected CancellableBackgroundTask(ILogger logger) {
		this.Logger = logger;
		this.CancellationToken = cancellationTokenSource.Token;
	}
	
	protected void Start() {
		Task.Run(Run, CancellationToken.None);
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
	
	protected abstract void Dispose();
	
	public void Stop() {
		try {
			cancellationTokenSource.Cancel();
		} catch (ObjectDisposedException) {
			// Ignore.
		}
	}
}
