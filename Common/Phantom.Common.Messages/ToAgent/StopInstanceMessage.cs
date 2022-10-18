using MemoryPack;
using Phantom.Common.Data.Minecraft;

namespace Phantom.Common.Messages.ToAgent; 

[MemoryPackable]
public sealed partial record StopInstanceMessage(
	[property: MemoryPackOrder(0)] uint SequenceId,
	[property: MemoryPackOrder(1)] Guid InstanceGuid,
	[property: MemoryPackOrder(2)] MinecraftStopStrategy StopStrategy
) : IMessageToAgent, IMessageWithReply {
	public Task Accept(IMessageToAgentListener listener) {
		return listener.HandleStopInstance(this);
	}
}
