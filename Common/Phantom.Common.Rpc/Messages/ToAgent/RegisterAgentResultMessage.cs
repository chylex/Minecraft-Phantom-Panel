using MessagePack;
using Phantom.Common.Data;
using Phantom.Common.Rpc.Message;

namespace Phantom.Common.Rpc.Messages.ToAgent;

[MessagePackObject]
public sealed record RegisterAgentResultMessage(
	[property: Key(0)] RegisterAgentResult Result
) : IMessageToAgent {
	public Task Accept(IMessageToAgentListener listener) {
		return listener.HandleAgentAuthenticationResult(this);
	}
}
