using MemoryPack;
using Phantom.Common.Data.Replies;
using Phantom.Utils.Rpc.Message;

namespace Phantom.Common.Messages.ToAgent;

[MemoryPackable]
public sealed partial record RegisterAgentFailureMessage(
	[property: MemoryPackOrder(0)] RegisterAgentFailure FailureKind
) : IMessageToAgent {
	public Task<NoReply> Accept(IMessageToAgentListener listener) {
		return listener.HandleRegisterAgentFailure(this);
	}
}
