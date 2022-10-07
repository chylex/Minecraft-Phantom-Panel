using System.Collections.Concurrent;
using Phantom.Utils.Collections;

namespace Phantom.Utils.Threading; 

public sealed class TaskManager {
	private readonly CancellationTokenSource cancellationTokenSource = new ();
	private readonly CancellationToken cancellationToken;
	
	private readonly ConcurrentDictionary<Task, object?> runningTasks = new (ReferenceEqualityComparer<Task>.Instance);
	
	public TaskManager() {
		cancellationToken = cancellationTokenSource.Token;
	}

	private T Add<T>(T task) where T : Task {
		cancellationToken.ThrowIfCancellationRequested();
		runningTasks.TryAdd(task, null);
		task.ContinueWith(OnFinished, CancellationToken.None, TaskContinuationOptions.RunContinuationsAsynchronously, TaskScheduler.Default);
		return task;
	}

	private void OnFinished(Task task) {
		runningTasks.TryRemove(task, out _);
	}

	public Task Run(Action action) {
		return Add(Task.Run(action, cancellationToken));
	}

	public Task Run(Func<Task> taskFunc) {
		return Add(Task.Run(taskFunc, cancellationToken));
	}

	public Task<T> Run<T>(Func<Task<T>> taskFunc) {
		return Add(Task.Run(taskFunc, cancellationToken));
	}

	public async Task Stop() {
		cancellationTokenSource.Cancel();
		
		foreach (var task in runningTasks.Keys) {
			try {
				await task;
			} catch (Exception) {
				// ignored
			}
		}
		
		runningTasks.Clear();
		cancellationTokenSource.Dispose();
	}
}
