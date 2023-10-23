using MemoryPack;
using Phantom.Utils.Rpc.Message;

namespace Phantom.Common.Messages.Agent.ToController;

[MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record UnregisterAgentMessage : IMessageToController {
	public Task<NoReply> Accept(IMessageToControllerListener listener) {
		return listener.HandleUnregisterAgent(this);
	}
}
