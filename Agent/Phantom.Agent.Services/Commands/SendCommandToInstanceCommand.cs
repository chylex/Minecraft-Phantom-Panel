using Phantom.Agent.Services.Command;

namespace Phantom.Agent.Services.Commands; 

sealed record SendCommandToInstanceCommand(Guid InstanceGuid, string Command) : BaseCommand<InstanceSessionManager.SendCommandResult> {
	protected override async Task<InstanceSessionManager.SendCommandResult> Run(AgentServices agent) {
		return await agent.InstanceSessionManager.SendCommand(InstanceGuid, Command);
	}

	protected override void Report(CommandListener listener, InstanceSessionManager.SendCommandResult result) {
		listener.OnSendCommandToInstance(result);
	}
}
