using MemoryPack;
using Phantom.Common.Data.Agent;
using Phantom.Utils.Rpc.Message;

namespace Phantom.Common.Messages.ToServer;

[MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record RegisterAgentMessage(
	[property: MemoryPackOrder(0)] AgentAuthToken AuthToken,
	[property: MemoryPackOrder(1)] AgentInfo AgentInfo
) : IMessageToServer {
	public Task<NoReply> Accept(IMessageToServerListener listener) {
		return listener.HandleRegisterAgent(this);
	}
}
