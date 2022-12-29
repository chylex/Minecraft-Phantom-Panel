using MemoryPack;
using Phantom.Common.Data.Replies;

namespace Phantom.Common.Messages.ToAgent; 

[MemoryPackable]
public sealed partial record LaunchInstanceMessage(
	[property: MemoryPackOrder(0)] uint SequenceId,
	[property: MemoryPackOrder(1)] Guid InstanceGuid
) : IMessageToAgent<InstanceActionResult<LaunchInstanceResult>> {
	public Task<InstanceActionResult<LaunchInstanceResult>> Accept(IMessageToAgentListener listener) {
		return listener.HandleLaunchInstance(this);
	}
}
