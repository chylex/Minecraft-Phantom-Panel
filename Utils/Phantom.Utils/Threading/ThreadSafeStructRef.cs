namespace Phantom.Utils.Threading;

public sealed class ThreadSafeStructRef<T> : IDisposable where T : struct {
	private T? value;
	private readonly SemaphoreSlim semaphore = new (1, 1);
	
	public async Task<T?> Get(CancellationToken cancellationToken) {
		await semaphore.WaitAsync(cancellationToken);
		try {
			return value;
		} finally {
			semaphore.Release();
		}
	}

	public async Task Set(T? value, CancellationToken cancellationToken) {
		await semaphore.WaitAsync(cancellationToken);
		try {
			this.value = value;
		} finally {
			semaphore.Release();
		}
	}

	public void Dispose() {
		semaphore.Dispose();
	}
}
