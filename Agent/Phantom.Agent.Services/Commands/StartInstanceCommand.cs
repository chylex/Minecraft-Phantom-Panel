using Phantom.Agent.Services.Command;

namespace Phantom.Agent.Services.Commands;

sealed record StartInstanceCommand(Guid InstanceGuid) : BaseCommand<InstanceSessionManager.LaunchResult> {
	protected override async Task<InstanceSessionManager.LaunchResult> Run(AgentServices agent) {
		return await agent.InstanceSessionManager.Start(InstanceGuid);
	}

	protected override void Report(CommandListener listener, InstanceSessionManager.LaunchResult result) {
		listener.OnStartInstance(result);
	}
}
