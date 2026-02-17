using Phantom.Agent.Services.Instances.State;

namespace Phantom.Agent.Services.Instances.Launch;

abstract record InstanceLaunchResult {
	private InstanceLaunchResult() {}
	
	public sealed record Success(InstanceProcess Process) : InstanceLaunchResult;
	
	public sealed record CouldNotPrepareServerInstance : InstanceLaunchResult;
	
	public sealed record CouldNotFindServerExecutable : InstanceLaunchResult;
	
	public sealed record CouldNotStartServerExecutable : InstanceLaunchResult;
}
