using MemoryPack;

namespace Phantom.Common.Messages.ToServer; 

[MemoryPackable]
public sealed partial record AgentIsAliveMessage : IMessageToServer {
	public Task Accept(IMessageToServerListener listener) {
		return listener.HandleAgentIsAlive(this);
	}
}
