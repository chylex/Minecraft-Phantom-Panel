namespace Phantom.Utils.Threading;

public sealed class ThreadSafeLinkedList<T> : IDisposable {
	private readonly LinkedList<T> list = new ();
	private readonly SemaphoreSlim semaphore = new (1, 1);

	public async Task Add(T item, bool toFront, CancellationToken cancellationToken) {
		await semaphore.WaitAsync(cancellationToken);
		try {
			if (toFront) {
				list.AddFirst(item);
			}
			else {
				list.AddLast(item);
			}
		} finally {
			semaphore.Release();
		}
	}

	public async Task<T?> TryTakeFromFront(CancellationToken cancellationToken) {
		await semaphore.WaitAsync(cancellationToken);
		try {
			var firstNode = list.First;
			if (firstNode == null) {
				return default;
			}

			list.RemoveFirst();
			return firstNode.Value;
		} finally {
			semaphore.Release();
		}
	}

	public void Dispose() {
		semaphore.Dispose();
	}
}
