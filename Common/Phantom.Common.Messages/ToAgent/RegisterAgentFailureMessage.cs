using MessagePack;
using Phantom.Common.Data.Replies;

namespace Phantom.Common.Messages.ToAgent;

[MessagePackObject]
public sealed record RegisterAgentFailureMessage(
	[property: Key(0)] RegisterAgentFailure FailureKind
) : IMessageToAgent {
	public Task Accept(IMessageToAgentListener listener) {
		return listener.HandleRegisterAgentFailureResult(this);
	}
}
