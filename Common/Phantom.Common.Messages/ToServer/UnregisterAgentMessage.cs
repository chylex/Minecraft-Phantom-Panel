using MessagePack;

namespace Phantom.Common.Messages.ToServer;

[MessagePackObject]
public sealed record UnregisterAgentMessage(
	[property: Key(0)] Guid AgentGuid
) : IMessageToServer {
	public Task Accept(IMessageToServerListener listener) {
		return listener.HandleUnregisterAgent(this);
	}
}
