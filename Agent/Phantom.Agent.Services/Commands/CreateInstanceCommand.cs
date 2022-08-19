using Phantom.Agent.Minecraft.Properties;
using Phantom.Agent.Services.Command;
using Phantom.Common.Data;

namespace Phantom.Agent.Services.Commands; 

sealed record CreateInstanceCommand(InstanceInfo Instance, ushort RconPort) : BaseCommand<Guid> {
	protected override Task<Guid> Run(AgentServices agent) {
		var serverProperties = new ServerProperties(Instance.ServerPort, Instance.RconPort);
		// agent.InstanceSessionManager.Create(Instance, serverProperties);

		return Task.FromResult(Guid.Empty); // TODO
	}

	protected override void Report(CommandListener listener, Guid result) {
		listener.OnCreateInstance(result);
	}
}
