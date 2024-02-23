using MemoryPack;
using Phantom.Common.Data.Instance;
using Phantom.Common.Data.Replies;

namespace Phantom.Common.Messages.Agent.ToAgent;

[MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record ConfigureInstanceMessage(
	[property: MemoryPackOrder(0)] Guid InstanceGuid,
	[property: MemoryPackOrder(1)] InstanceConfiguration Configuration,
	[property: MemoryPackOrder(2)] InstanceLaunchProperties LaunchProperties,
	[property: MemoryPackOrder(3)] bool LaunchNow = false
) : IMessageToAgent<InstanceActionResult<ConfigureInstanceResult>> {
	public Task<InstanceActionResult<ConfigureInstanceResult>> Accept(IMessageToAgentListener listener) {
		return listener.HandleConfigureInstance(this);
	}
}
