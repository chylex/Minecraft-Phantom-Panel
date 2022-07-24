using Phantom.Agent.Command;

namespace Phantom.Agent.Commands; 

sealed class SendCommandToInstanceCommand : BaseCommand<InstanceManager.SendCommandResult> {
	private readonly Guid instanceGuid;
	private readonly string command;
	
	public SendCommandToInstanceCommand(Guid instanceGuid, string command) {
		this.instanceGuid = instanceGuid;
		this.command = command;
	}

	protected override async Task<InstanceManager.SendCommandResult> Run(AgentServices agent) {
		return await agent.InstanceManager.SendCommand(instanceGuid, command);
	}

	protected override void Report(CommandListener listener, InstanceManager.SendCommandResult result) {
		listener.OnSendCommandToInstance(result);
	}
}
