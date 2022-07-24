using Phantom.Agent.Command;
using Phantom.Agent.Minecraft.Properties;

namespace Phantom.Agent.Commands; 

sealed record CreateInstanceCommand(ushort ServerPort, ushort RconPort) : BaseCommand<Guid> {
	protected override Task<Guid> Run(AgentServices agent) {
		var serverProperties = new ServerProperties(ServerPort, RconPort);
		var guid = agent.InstanceManager.Create(serverProperties);

		return Task.FromResult(guid);
	}

	protected override void Report(CommandListener listener, Guid result) {
		listener.OnCreateInstance(result);
	}
}
