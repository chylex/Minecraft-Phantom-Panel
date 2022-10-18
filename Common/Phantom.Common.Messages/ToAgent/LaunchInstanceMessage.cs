using MemoryPack;

namespace Phantom.Common.Messages.ToAgent; 

[MemoryPackable]
public sealed partial record LaunchInstanceMessage(
	[property: MemoryPackOrder(0)] uint SequenceId,
	[property: MemoryPackOrder(1)] Guid InstanceGuid
) : IMessageToAgent, IMessageWithReply {
	public Task Accept(IMessageToAgentListener listener) {
		return listener.HandleLaunchInstance(this);
	}
}
