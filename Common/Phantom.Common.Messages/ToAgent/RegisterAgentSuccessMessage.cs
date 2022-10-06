using MessagePack;

namespace Phantom.Common.Messages.ToAgent;

[MessagePackObject]
public sealed record RegisterAgentSuccessMessage : IMessageToAgent {
	public Task Accept(IMessageToAgentListener listener) {
		return listener.HandleRegisterAgentSuccessResult(this);
	}
}
