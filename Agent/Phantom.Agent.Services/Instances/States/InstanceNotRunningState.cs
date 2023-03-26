namespace Phantom.Agent.Services.Instances.States; 

sealed class InstanceNotRunningState : IInstanceState {
	public void Initialize() {}

	public Task<bool> SendCommand(string command, CancellationToken cancellationToken) {
		return Task.FromResult(false);
	}
}
