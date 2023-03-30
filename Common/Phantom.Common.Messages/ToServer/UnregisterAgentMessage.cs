using MemoryPack;
using Phantom.Utils.Rpc.Message;

namespace Phantom.Common.Messages.ToServer;

[MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record UnregisterAgentMessage(
	[property: MemoryPackOrder(0)] Guid AgentGuid
) : IMessageToServer {
	public Task<NoReply> Accept(IMessageToServerListener listener) {
		return listener.HandleUnregisterAgent(this);
	}
}
