namespace Phantom.Utils.Tasks;

public static class AsyncTasks {
	public static TaskCompletionSource CreateCompletionSource() {
		return new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
	}

	public static TaskCompletionSource<T> CreateCompletionSource<T>() {
		return new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
	}

	public static void SetResultFrom(this TaskCompletionSource completionSource, Task task) {
		if (task.IsFaulted) {
			completionSource.SetException(task.Exception.InnerExceptions);
		}
		else if (task.IsCanceled) {
			completionSource.SetCanceled();
		}
		else {
			completionSource.SetResult();
		}
	}
	
	public static void SetResultFrom<T>(this TaskCompletionSource<T> completionSource, Task<T> task) {
		if (task.IsFaulted) {
			completionSource.SetException(task.Exception.InnerExceptions);
		}
		else if (task.IsCanceled) {
			completionSource.SetCanceled();
		}
		else {
			completionSource.SetResult(task.Result);
		}
	}
}
