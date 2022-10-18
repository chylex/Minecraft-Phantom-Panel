using MemoryPack;
using Phantom.Common.Data.Agent;

namespace Phantom.Common.Messages.ToServer;

[MemoryPackable]
public sealed partial record RegisterAgentMessage(
	[property: MemoryPackOrder(0)] AgentAuthToken AuthToken,
	[property: MemoryPackOrder(1)] AgentInfo AgentInfo
) : IMessageToServer {
	public Task Accept(IMessageToServerListener listener) {
		return listener.HandleRegisterAgent(this);
	}
}
