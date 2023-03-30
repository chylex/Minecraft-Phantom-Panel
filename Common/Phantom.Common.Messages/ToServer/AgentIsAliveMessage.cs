using MemoryPack;
using Phantom.Utils.Rpc.Message;

namespace Phantom.Common.Messages.ToServer; 

[MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record AgentIsAliveMessage : IMessageToServer {
	public Task<NoReply> Accept(IMessageToServerListener listener) {
		return listener.HandleAgentIsAlive(this);
	}
}
