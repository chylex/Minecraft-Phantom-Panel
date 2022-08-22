using MessagePack;

namespace Phantom.Common.Messages.ToAgent; 

[MessagePackObject]
public sealed record SendCommandToInstanceMessage(
	[property: Key(0)] uint SequenceId,
	[property: Key(1)] Guid InstanceGuid,
	[property: Key(2)] string Command
) : IMessageToAgent, IMessageWithReply {
	public Task Accept(IMessageToAgentListener listener) {
		return listener.HandleSendCommandToInstance(this);
	}
}
