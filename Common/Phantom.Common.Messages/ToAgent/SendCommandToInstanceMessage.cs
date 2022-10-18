using MemoryPack;

namespace Phantom.Common.Messages.ToAgent; 

[MemoryPackable]
public sealed partial record SendCommandToInstanceMessage(
	[property: MemoryPackOrder(0)] uint SequenceId,
	[property: MemoryPackOrder(1)] Guid InstanceGuid,
	[property: MemoryPackOrder(2)] string Command
) : IMessageToAgent, IMessageWithReply {
	public Task Accept(IMessageToAgentListener listener) {
		return listener.HandleSendCommandToInstance(this);
	}
}
