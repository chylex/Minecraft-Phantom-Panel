namespace Phantom.Utils.Tasks;

public sealed class LinkedTasks<T>(Task<T>[] tasks) {
	public async Task CancelTokenWhenAnyCompletes(CancellationTokenSource cancellationTokenSource) {
		await Task.WhenAny(tasks);
		await cancellationTokenSource.CancelAsync();
	}
	
	public async Task<Task<T>[]> WaitForAll() {
		await Task.WhenAll(tasks);
		return tasks;
	}
}
