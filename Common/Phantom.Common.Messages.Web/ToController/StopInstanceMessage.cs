using MemoryPack;
using Phantom.Common.Data.Minecraft;
using Phantom.Common.Data.Replies;

namespace Phantom.Common.Messages.Web.ToController; 

[MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record StopInstanceMessage(
	[property: MemoryPackOrder(0)] Guid LoggedInUserGuid,
	[property: MemoryPackOrder(1)] Guid InstanceGuid,
	[property: MemoryPackOrder(2)] MinecraftStopStrategy StopStrategy
) : IMessageToController<InstanceActionResult<StopInstanceResult>> {
	public Task<InstanceActionResult<StopInstanceResult>> Accept(IMessageToControllerListener listener) {
		return listener.HandleStopInstance(this);
	}
}
