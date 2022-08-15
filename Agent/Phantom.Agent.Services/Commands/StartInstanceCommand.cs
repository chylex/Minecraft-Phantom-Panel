using Phantom.Agent.Services.Command;

namespace Phantom.Agent.Services.Commands;

public sealed record StartInstanceCommand(Guid InstanceGuid) : BaseCommand<InstanceManager.LaunchResult> {
	protected override async Task<InstanceManager.LaunchResult> Run(AgentServices agent) {
		return await agent.InstanceManager.Start(InstanceGuid);
	}

	protected override void Report(CommandListener listener, InstanceManager.LaunchResult result) {
		listener.OnStartInstance(result);
	}
}
