using MessagePack;
using Phantom.Common.Data.Agent;

namespace Phantom.Common.Messages.ToServer;

[MessagePackObject]
public sealed record RegisterAgentMessage(
	[property: Key(0)] AgentAuthToken AuthToken,
	[property: Key(1)] AgentInfo AgentInfo
) : IMessageToServer {
	public Task Accept(IMessageToServerListener listener) {
		return listener.HandleRegisterAgent(this);
	}
}
