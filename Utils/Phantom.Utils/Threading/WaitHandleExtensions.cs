namespace Phantom.Utils.Threading;

public static class WaitHandleExtensions {
	public static Task WaitOneAsync(this WaitHandle waitHandle, CancellationToken cancellationToken = default) {
		var taskCompletionSource = new TaskCompletionSource();
		
		void SetResult(object? state, bool timedOut) {
			taskCompletionSource.TrySetResult();
		}
		
		void SetCancelled() {
			taskCompletionSource.TrySetCanceled(cancellationToken);
		}
		
		var waitRegistration = ThreadPool.RegisterWaitForSingleObject(waitHandle, SetResult, null, Timeout.InfiniteTimeSpan, true);
		var tokenRegistration = cancellationToken.Register(SetCancelled, useSynchronizationContext: false);
		
		void Cleanup(Task t) {
			waitRegistration.Unregister(null);
			tokenRegistration.Dispose();
		}
		
		var task = taskCompletionSource.Task;
		task.ContinueWith(Cleanup, CancellationToken.None);
		return task;
	}
}
