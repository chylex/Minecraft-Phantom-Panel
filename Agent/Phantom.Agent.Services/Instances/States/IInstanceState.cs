namespace Phantom.Agent.Services.Instances.States; 

interface IInstanceState {
	IInstanceState Launch(InstanceContext context);
	IInstanceState Stop();
}
