using MemoryPack;
using Phantom.Common.Data.Replies;

namespace Phantom.Common.Messages.ToAgent; 

[MemoryPackable]
public sealed partial record SendCommandToInstanceMessage(
	[property: MemoryPackOrder(0)] uint SequenceId,
	[property: MemoryPackOrder(1)] Guid InstanceGuid,
	[property: MemoryPackOrder(2)] string Command
) : IMessageToAgent<InstanceActionResult<SendCommandToInstanceResult>> {
	public Task<InstanceActionResult<SendCommandToInstanceResult>> Accept(IMessageToAgentListener listener) {
		return listener.HandleSendCommandToInstance(this);
	}
}
