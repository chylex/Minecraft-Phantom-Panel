using System.Collections.Immutable;
using MessagePack;
using Phantom.Common.Data.Instance;

namespace Phantom.Common.Messages.ToAgent;

[MessagePackObject]
public sealed record RegisterAgentSuccessMessage(
	[property: Key(0)] ImmutableArray<InstanceConfiguration> InitialInstances
) : IMessageToAgent {
	public Task Accept(IMessageToAgentListener listener) {
		return listener.HandleRegisterAgentSuccessResult(this);
	}
}
