using MessagePack;

namespace Phantom.Common.Messages.ToServer; 

[MessagePackObject]
public sealed record AgentIsAliveMessage : IMessageToServer {
	public Task Accept(IMessageToServerListener listener) {
		return listener.HandleAgentIsAlive(this);
	}
}
