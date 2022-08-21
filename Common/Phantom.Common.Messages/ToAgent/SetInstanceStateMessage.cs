using MessagePack;

namespace Phantom.Common.Messages.ToAgent; 

[MessagePackObject]
public sealed record SetInstanceStateMessage(
	[property: Key(0)] uint SequenceId,
	[property: Key(1)] Guid InstanceGuid,
	[property: Key(2)] bool? IsRunning = null
) : IMessageToAgent, IMessageWithReply {
	public Task Accept(IMessageToAgentListener listener) {
		return listener.HandleSetInstanceState(this);
	}
}
