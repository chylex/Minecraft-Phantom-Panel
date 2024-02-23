using MemoryPack;
using Phantom.Common.Data.Replies;

namespace Phantom.Common.Messages.Web.ToController; 

[MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record SendCommandToInstanceMessage(
	[property: MemoryPackOrder(0)] Guid LoggedInUserGuid,
	[property: MemoryPackOrder(1)] Guid AgentGuid,
	[property: MemoryPackOrder(2)] Guid InstanceGuid,
	[property: MemoryPackOrder(3)] string Command
) : IMessageToController<InstanceActionResult<SendCommandToInstanceResult>> {
	public Task<InstanceActionResult<SendCommandToInstanceResult>> Accept(IMessageToControllerListener listener) {
		return listener.HandleSendCommandToInstance(this);
	}
}
