using System.Threading.Channels;

namespace Phantom.Utils.Threading;

public sealed class WorkerPool {
	private readonly CancellationTokenSource cancellationTokenSource = new ();
	private readonly Channel<Action> workItemsChannel = Channel.CreateUnbounded<Action>();
	private readonly SemaphoreSlim workDoneSemaphore;

	public WorkerPool(int workerCount) {
		workDoneSemaphore = new SemaphoreSlim(workerCount);
		
		for (int i = 0; i < workerCount; i++) {
			Task.Run(RunWorker);
		}
	}

	private async Task RunWorker() {
		await foreach (var action in workItemsChannel.Reader.ReadAllAsync(cancellationTokenSource.Token)) {
			action();
		}
		
		workDoneSemaphore.Release();
	}
	
	public void AddWork(Action action) {
		workItemsChannel.Writer.TryWrite(action);
	}
	
	public async Task Stop() {
		workItemsChannel.Writer.Complete();
		cancellationTokenSource.Cancel();
		await workDoneSemaphore.WaitAsync();
		cancellationTokenSource.Dispose();
	}
}
