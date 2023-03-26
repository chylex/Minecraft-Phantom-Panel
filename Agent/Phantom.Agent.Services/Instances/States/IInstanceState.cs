namespace Phantom.Agent.Services.Instances.States; 

interface IInstanceState {
	void Initialize();
	Task<bool> SendCommand(string command, CancellationToken cancellationToken);
}
