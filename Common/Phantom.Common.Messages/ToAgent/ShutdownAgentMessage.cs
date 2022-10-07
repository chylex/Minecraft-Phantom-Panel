using MessagePack;

namespace Phantom.Common.Messages.ToAgent; 

[MessagePackObject]
public sealed record ShutdownAgentMessage : IMessageToAgent {
	public Task Accept(IMessageToAgentListener listener) {
		return listener.HandleShutdownAgent(this);
	}
}
