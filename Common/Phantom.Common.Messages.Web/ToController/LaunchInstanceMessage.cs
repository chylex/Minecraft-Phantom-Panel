using MemoryPack;
using Phantom.Common.Data.Replies;

namespace Phantom.Common.Messages.Web.ToController; 

[MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record LaunchInstanceMessage(
	[property: MemoryPackOrder(0)] Guid LoggedInUserGuid,
	[property: MemoryPackOrder(1)] Guid InstanceGuid
) : IMessageToController<InstanceActionResult<LaunchInstanceResult>> {
	public Task<InstanceActionResult<LaunchInstanceResult>> Accept(IMessageToControllerListener listener) {
		return listener.HandleLaunchInstance(this);
	}
}
