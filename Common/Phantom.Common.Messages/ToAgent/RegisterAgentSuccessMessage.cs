using System.Collections.Immutable;
using MemoryPack;
using Phantom.Common.Data.Instance;

namespace Phantom.Common.Messages.ToAgent;

[MemoryPackable]
public sealed partial record RegisterAgentSuccessMessage(
	[property: MemoryPackOrder(0)] ImmutableArray<InstanceConfiguration> InitialInstances
) : IMessageToAgent {
	public Task Accept(IMessageToAgentListener listener) {
		return listener.HandleRegisterAgentSuccessResult(this);
	}
}
