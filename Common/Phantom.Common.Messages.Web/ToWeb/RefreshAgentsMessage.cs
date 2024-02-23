using System.Collections.Immutable;
using MemoryPack;
using Phantom.Common.Data.Web.Agent;
using Phantom.Utils.Rpc.Message;

namespace Phantom.Common.Messages.Web.ToWeb; 

[MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record RefreshAgentsMessage(
	[property: MemoryPackOrder(0)] ImmutableArray<Agent> Agents
) : IMessageToWeb {
	public Task<NoReply> Accept(IMessageToWebListener listener) {
		return listener.HandleRefreshAgents(this);
	}
}
