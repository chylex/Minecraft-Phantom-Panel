using System.Collections.Immutable;
using MemoryPack;
using Phantom.Common.Data.Instance;
using Phantom.Utils.Rpc.Message;

namespace Phantom.Common.Messages.ToAgent;

[MemoryPackable]
public sealed partial record RegisterAgentSuccessMessage(
	[property: MemoryPackOrder(0)] ImmutableArray<InstanceConfiguration> InitialInstances
) : IMessageToAgent {
	public Task<NoReply> Accept(IMessageToAgentListener listener) {
		return listener.HandleRegisterAgentSuccess(this);
	}
}
