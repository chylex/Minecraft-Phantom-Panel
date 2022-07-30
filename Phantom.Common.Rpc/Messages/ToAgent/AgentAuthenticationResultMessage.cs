using MessagePack;
using Phantom.Common.Rpc.Message;

namespace Phantom.Common.Rpc.Messages.ToAgent;

[MessagePackObject]
public record AgentAuthenticationResultMessage(
	[property: Key(0)] bool Success,
	[property: Key(1)] string? ErrorMessage
) : IMessageToAgent {
	public Task Accept(IMessageToAgentListener listener) {
		return listener.HandleAgentAuthenticationResult(this);
	}
}
