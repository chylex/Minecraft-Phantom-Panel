using System.Threading.Channels;
using Phantom.Utils.Tasks;

namespace Phantom.Utils.Rpc; 

sealed class RpcQueue {
	private readonly Channel<Func<Task>> channel = Channel.CreateUnbounded<Func<Task>>(new UnboundedChannelOptions {
		SingleReader = true,
		SingleWriter = false,
		AllowSynchronousContinuations = false
	});
	
	private readonly Task processingTask;

	public RpcQueue(TaskManager taskManager, string taskName) {
		this.processingTask = taskManager.Run(taskName, Process);
	}

	public Task Enqueue(Action action) {
		return Enqueue(() => {
			action();
			return Task.CompletedTask;
		});
	}
	
	public Task Enqueue(Func<Task> task) {
		var completionSource = AsyncTasks.CreateCompletionSource();
		
		if (!channel.Writer.TryWrite(() => task().ContinueWith(t => completionSource.SetResultFrom(t)))) {
			completionSource.SetCanceled();
		}
		
		return completionSource.Task;
	}
	
	public Task<T> Enqueue<T>(Func<Task<T>> task) {
		var completionSource = AsyncTasks.CreateCompletionSource<T>();
		
		if (!channel.Writer.TryWrite(() => task().ContinueWith(t => completionSource.SetResultFrom(t)))) {
			completionSource.SetCanceled();
		}
		
		return completionSource.Task;
	}

	private async Task Process() {
		try {
			await foreach (var task in channel.Reader.ReadAllAsync()) {
				await task();
			}
		} catch (OperationCanceledException) {
			// Ignore.
		}
	}

	public Task Stop() {
		channel.Writer.Complete();
		return processingTask;
	}
}
