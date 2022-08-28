using System.Collections.Immutable;
using MessagePack;
using Phantom.Common.Data;

namespace Phantom.Common.Messages.ToAgent;

[MessagePackObject]
public sealed record RegisterAgentSuccessMessage(
	[property: Key(0)] ImmutableArray<InstanceInfo> InitialInstances
) : IMessageToAgent {
	public Task Accept(IMessageToAgentListener listener) {
		return listener.HandleRegisterAgentSuccessResult(this);
	}
}
