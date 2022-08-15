using MessagePack;
using Phantom.Common.Rpc.Message;

namespace Phantom.Common.Rpc.Messages.ToAgent; 

[MessagePackObject]
public sealed record ShutdownAgentMessage : IMessageToAgent {
	public Task Accept(IMessageToAgentListener listener) {
		return listener.HandleShutdownAgent(this);
	}
}
