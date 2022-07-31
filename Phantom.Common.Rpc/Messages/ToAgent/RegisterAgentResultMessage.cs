using MessagePack;
using Phantom.Common.Rpc.Message;

namespace Phantom.Common.Rpc.Messages.ToAgent;

[MessagePackObject]
public sealed record RegisterAgentResultMessage(
	[property: Key(0)] bool Success,
	[property: Key(1)] string? ErrorMessage
) : IMessageToAgent {
	public static RegisterAgentResultMessage WithSuccess { get; } = new (Success: true, ErrorMessage: null);
	public static RegisterAgentResultMessage WithError(string errorMessage) => new (Success: false, errorMessage);

	public Task Accept(IMessageToAgentListener listener) {
		return listener.HandleAgentAuthenticationResult(this);
	}
}
