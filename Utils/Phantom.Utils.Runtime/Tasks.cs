namespace Phantom.Utils.Runtime; 

public static class Tasks {
	public static TaskCompletionSource CreateCompletionSource() {
		return new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
	}

	public static TaskCompletionSource<T> CreateCompletionSource<T>() {
		return new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
	}
}
