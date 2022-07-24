using Phantom.Agent.Command;

namespace Phantom.Agent.Commands; 

sealed class StartInstanceCommand : BaseCommand<InstanceManager.LaunchResult> {
	private readonly Guid instanceGuid;
	
	public StartInstanceCommand(Guid instanceGuid) {
		this.instanceGuid = instanceGuid;
	}

	protected override Task<InstanceManager.LaunchResult> Run(AgentServices agent) {
		return Task.FromResult(agent.InstanceManager.Start(instanceGuid));
	}

	protected override void Report(CommandListener listener, InstanceManager.LaunchResult result) {
		listener.OnStartInstance(result);
	}
}
