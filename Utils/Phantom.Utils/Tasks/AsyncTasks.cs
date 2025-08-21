namespace Phantom.Utils.Tasks;

public static class AsyncTasks {
	public static TaskCompletionSource CreateCompletionSource() {
		return new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
	}
	
	public static TaskCompletionSource<T> CreateCompletionSource<T>() {
		return new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
	}
}
