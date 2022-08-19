using MessagePack;

namespace Phantom.Common.Messages.ToAgent; 

[MessagePackObject]
public sealed record SetInstanceStateMessage(
	[property: Key(0)] Guid InstanceGuid,
	[property: Key(1)] bool? IsRunning = null
) : IMessageToAgent {
	public Task Accept(IMessageToAgentListener listener) {
		return listener.HandleSetInstanceState(this);
	}
}
