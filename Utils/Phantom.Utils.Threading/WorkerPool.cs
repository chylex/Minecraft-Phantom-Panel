using System.Threading.Channels;

namespace Phantom.Utils.Threading;

public sealed class WorkerPool {
	private readonly CancellationTokenSource cancellationTokenSource = new ();
	private readonly Channel<Func<Task>> workItemsChannel = Channel.CreateUnbounded<Func<Task>>();
	private readonly Task[] workTasks;

	public WorkerPool(int workerCount) {
		workTasks = new Task[workerCount];
		
		for (int i = 0; i < workerCount; i++) {
			workTasks[i] = Task.Run(RunWorker);
		}
	}

	private async Task RunWorker() {
		await foreach (var action in workItemsChannel.Reader.ReadAllAsync(cancellationTokenSource.Token)) {
			await action();
		}
	}
	
	public void AddWork(Func<Task> action) {
		workItemsChannel.Writer.TryWrite(action);
	}
	
	public async Task Stop() {
		workItemsChannel.Writer.Complete();
		cancellationTokenSource.Cancel();
		await Task.WhenAll(workTasks);
		cancellationTokenSource.Dispose();
	}
}
