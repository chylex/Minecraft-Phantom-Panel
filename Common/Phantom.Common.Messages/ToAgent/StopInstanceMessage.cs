using MessagePack;
using Phantom.Common.Data.Minecraft;

namespace Phantom.Common.Messages.ToAgent; 

[MessagePackObject]
public sealed record StopInstanceMessage(
	[property: Key(0)] uint SequenceId,
	[property: Key(1)] Guid InstanceGuid,
	[property: Key(2)] MinecraftStopStrategy StopStrategy
) : IMessageToAgent, IMessageWithReply {
	public Task Accept(IMessageToAgentListener listener) {
		return listener.HandleStopInstance(this);
	}
}
