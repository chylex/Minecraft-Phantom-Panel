using Phantom.Agent.Command;

namespace Phantom.Agent.Commands; 

sealed record SendCommandToInstanceCommand(Guid InstanceGuid, string Command) : BaseCommand<InstanceManager.SendCommandResult> {
	protected override async Task<InstanceManager.SendCommandResult> Run(AgentServices agent) {
		return await agent.InstanceManager.SendCommand(InstanceGuid, Command);
	}

	protected override void Report(CommandListener listener, InstanceManager.SendCommandResult result) {
		listener.OnSendCommandToInstance(result);
	}
}
