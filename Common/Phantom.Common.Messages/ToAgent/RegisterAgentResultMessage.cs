using MessagePack;
using Phantom.Common.Data;

namespace Phantom.Common.Messages.ToAgent;

[MessagePackObject]
public sealed record RegisterAgentResultMessage(
	[property: Key(0)] RegisterAgentResult Result
) : IMessageToAgent {
	public Task Accept(IMessageToAgentListener listener) {
		return listener.HandleAgentAuthenticationResult(this);
	}
}
