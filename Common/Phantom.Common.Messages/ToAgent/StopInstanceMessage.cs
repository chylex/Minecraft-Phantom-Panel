using MemoryPack;
using Phantom.Common.Data.Minecraft;
using Phantom.Common.Data.Replies;

namespace Phantom.Common.Messages.ToAgent;

[MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record StopInstanceMessage(
	[property: MemoryPackOrder(0)] Guid InstanceGuid,
	[property: MemoryPackOrder(1)] MinecraftStopStrategy StopStrategy
) : IMessageToAgent<InstanceActionResult<StopInstanceResult>> {
	public Task<InstanceActionResult<StopInstanceResult>> Accept(IMessageToAgentListener listener) {
		return listener.HandleStopInstance(this);
	}
}
