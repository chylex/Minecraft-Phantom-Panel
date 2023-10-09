using System.Collections.Immutable;
using MemoryPack;
using Phantom.Utils.Rpc.Message;

namespace Phantom.Common.Messages.Agent.ToAgent;

[MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record RegisterAgentSuccessMessage(
	[property: MemoryPackOrder(0)] ImmutableArray<ConfigureInstanceMessage> InitialInstanceConfigurations
) : IMessageToAgent {
	public Task<NoReply> Accept(IMessageToAgentListener listener) {
		return listener.HandleRegisterAgentSuccess(this);
	}
}
