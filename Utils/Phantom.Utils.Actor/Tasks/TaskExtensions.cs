using Akka.Dispatch;

namespace Phantom.Utils.Actor.Tasks;

public static class TaskExtensions {
	public static Task<TResult> ContinueOnActor<TSource, TResult>(this Task<TSource> task, Func<TSource, TResult> mapper) {
		if (TaskScheduler.Current is not ActorTaskScheduler actorTaskScheduler) {
			throw new InvalidOperationException("Task must be scheduled in Actor context!");
		}
		
		var continuationCompletionSource = new TaskCompletionSource<TResult>();
		var continuationTask = task.ContinueWith(t => MapResult(t, mapper, continuationCompletionSource), CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, actorTaskScheduler);
		return continuationTask.Unwrap();
	}
	
	public static Task<TResult> ContinueOnActor<TSource, TArg, TResult>(this Task<TSource> task, Func<TSource, TArg, TResult> mapper, TArg arg) {
		return task.ContinueOnActor(result => mapper(result, arg));
	}
	
	public static Task<TResult> ContinueOnActor<TSource, TArg1, TArg2, TResult>(this Task<TSource> task, Func<TSource, TArg1, TArg2, TResult> mapper, TArg1 arg1, TArg2 arg2) {
		return task.ContinueOnActor(result => mapper(result, arg1, arg2));
	}
	
	private static Task<TResult> MapResult<TSource, TResult>(Task<TSource> task, Func<TSource, TResult> mapper, TaskCompletionSource<TResult> completionSource) {
		if (task.IsFaulted) {
			completionSource.SetException(task.Exception.InnerExceptions);
		}
		else if (task.IsCanceled) {
			completionSource.SetCanceled();
		}
		else {
			completionSource.SetResult(mapper(task.Result));
		}
		
		return completionSource.Task;
	}
}
