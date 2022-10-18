using MemoryPack;
using Phantom.Common.Data.Instance;

namespace Phantom.Common.Messages.ToAgent;

[MemoryPackable]
public sealed partial record ConfigureInstanceMessage(
	[property: MemoryPackOrder(0)] uint SequenceId,
	[property: MemoryPackOrder(1)] InstanceConfiguration Configuration
) : IMessageToAgent, IMessageWithReply {
	public Task Accept(IMessageToAgentListener listener) {
		return listener.HandleConfigureInstance(this);
	}
}
