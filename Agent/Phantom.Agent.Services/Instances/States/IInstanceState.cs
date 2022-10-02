namespace Phantom.Agent.Services.Instances.States; 

interface IInstanceState {
	IInstanceState Launch(InstanceContext context);
	IInstanceState Stop(InstanceContext context);
	Task<bool> SendCommand(string command, CancellationToken cancellationToken);
}
