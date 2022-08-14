using MessagePack;
using Phantom.Common.Data;
using Phantom.Common.Rpc.Message;

namespace Phantom.Common.Rpc.Messages.ToServer;

[MessagePackObject]
public sealed record RegisterAgentMessage(
	[property: Key(0)] string AuthToken,
	[property: Key(1)] AgentInfo AgentInfo
) : IMessageToServer {
	public Task Accept(IMessageToServerListener listener) {
		return listener.HandleAgentAuthentication(this);
	}
}
