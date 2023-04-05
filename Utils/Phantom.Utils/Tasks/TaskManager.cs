using System.Collections.Concurrent;
using Phantom.Utils.Collections;
using Serilog;

namespace Phantom.Utils.Tasks; 

public sealed class TaskManager {
	private readonly ILogger logger;
	private readonly CancellationTokenSource cancellationTokenSource = new ();
	private readonly CancellationToken cancellationToken;
	
	private readonly ConcurrentDictionary<Task, string> runningTasks = new (ReferenceEqualityComparer<Task>.Instance);
	
	public TaskManager(ILogger logger) {
		this.logger = logger;
		this.cancellationToken = cancellationTokenSource.Token;
	}

	private T Add<T>(string name, T task) where T : Task {
		cancellationToken.ThrowIfCancellationRequested();
		runningTasks.TryAdd(task, name);
		task.ContinueWith(OnFinished, CancellationToken.None, TaskContinuationOptions.RunContinuationsAsynchronously, TaskScheduler.Default);
		return task;
	}

	private void OnFinished(Task task) {
		runningTasks.TryRemove(task, out _);
	}

	public Task Run(string name, Action action) {
		return Add(name, Task.Run(action, cancellationToken));
	}

	public Task Run(string name, Func<Task> taskFunc) {
		return Add(name, Task.Run(taskFunc, cancellationToken));
	}

	public Task<T> Run<T>(string name, Func<Task<T>> taskFunc) {
		return Add(name, Task.Run(taskFunc, cancellationToken));
	}

	public async Task Stop() {
		logger.Information("Stopping task manager...");
		
		cancellationTokenSource.Cancel();

		var remainingTasksAwaiterTask = WaitForRemainingTasks();
		while (true) {
			var logStateTimeoutTask = Task.Delay(TimeSpan.FromSeconds(10), CancellationToken.None);
			var completedTask = await Task.WhenAny(remainingTasksAwaiterTask, logStateTimeoutTask);
			if (completedTask == logStateTimeoutTask) {
				var remainingTaskNames = runningTasks.Values.Order().ToList();
				var remainingTaskNameList = string.Join('\n', remainingTaskNames.Select(static name => "- " + name));
				logger.Warning("Waiting for {TaskCount} task(s) to finish:\n{TaskNames}", remainingTaskNames.Count, remainingTaskNameList);
			}
			else {
				break;
			}
		}

		runningTasks.Clear();
		cancellationTokenSource.Dispose();
		
		logger.Information("Task manager stopped.");
	}

	private async Task WaitForRemainingTasks() {
		foreach (var task in runningTasks.Keys) {
			try {
				await task;
			} catch (Exception) {
				// ignored
			}
		}
	}
}
