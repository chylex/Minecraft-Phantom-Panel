namespace Phantom.Utils.Runtime; 

public sealed class CancellableSemaphore : IDisposable {
	private readonly SemaphoreSlim semaphore;
	private readonly CancellationTokenSource cancellationTokenSource = new ();
	
	public CancellableSemaphore(int initialCount, int maxCount) {
		this.semaphore = new SemaphoreSlim(initialCount, maxCount);
	}

	public async Task<bool> Wait(TimeSpan timeout, CancellationToken cancellationToken) {
		return await semaphore.WaitAsync(timeout, cancellationTokenSource.Token).WaitAsync(cancellationToken);
	}

	public async Task<bool> CancelAndWait(TimeSpan timeout) {
		cancellationTokenSource.Cancel();
		return await semaphore.WaitAsync(timeout);
	}

	public void Release() {
		semaphore.Release();
	}
	
	public void Dispose() {
		semaphore.Dispose();
		cancellationTokenSource.Dispose();
	}
}
