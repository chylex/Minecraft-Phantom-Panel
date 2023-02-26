using MemoryPack;
using Phantom.Common.Data.Instance;
using Phantom.Common.Data.Replies;

namespace Phantom.Common.Messages.ToAgent;

[MemoryPackable]
public sealed partial record ConfigureInstanceMessage(
	[property: MemoryPackOrder(0)] InstanceConfiguration Configuration,
	[property: MemoryPackOrder(1)] InstanceLaunchProperties LaunchProperties
) : IMessageToAgent<InstanceActionResult<ConfigureInstanceResult>> {
	public Task<InstanceActionResult<ConfigureInstanceResult>> Accept(IMessageToAgentListener listener) {
		return listener.HandleConfigureInstance(this);
	}
}
