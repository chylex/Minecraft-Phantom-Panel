using MemoryPack;

namespace Phantom.Common.Messages.ToServer;

[MemoryPackable]
public sealed partial record UnregisterAgentMessage(
	[property: MemoryPackOrder(0)] Guid AgentGuid
) : IMessageToServer {
	public Task Accept(IMessageToServerListener listener) {
		return listener.HandleUnregisterAgent(this);
	}
}
