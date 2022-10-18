using MemoryPack;
using Phantom.Common.Data.Replies;

namespace Phantom.Common.Messages.ToAgent;

[MemoryPackable]
public sealed partial record RegisterAgentFailureMessage(
	[property: MemoryPackOrder(0)] RegisterAgentFailure FailureKind
) : IMessageToAgent {
	public Task Accept(IMessageToAgentListener listener) {
		return listener.HandleRegisterAgentFailureResult(this);
	}
}
