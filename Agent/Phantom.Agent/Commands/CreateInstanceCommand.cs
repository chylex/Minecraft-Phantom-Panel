using Phantom.Agent.Command;
using Phantom.Agent.Minecraft.Properties;

namespace Phantom.Agent.Commands; 

sealed class CreateInstanceCommand : BaseCommand<Guid> {
	private readonly ushort serverPort;
	private readonly ushort rconPort;
	
	public CreateInstanceCommand(ushort serverPort, ushort rconPort) {
		this.serverPort = serverPort;
		this.rconPort = rconPort;
	}

	protected override Task<Guid> Run(AgentServices agent) {
		var serverProperties = new ServerProperties(serverPort, rconPort);
		var guid = agent.InstanceManager.Create(serverProperties);

		return Task.FromResult(guid);
	}

	protected override void Report(CommandListener listener, Guid result) {
		listener.OnCreateInstance(result);
	}
}
