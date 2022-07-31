using MessagePack;
using Phantom.Common.Rpc.Message;

namespace Phantom.Common.Rpc.Messages.ToServer;

[MessagePackObject]
public sealed record RegisterAgentMessage(
	[property: Key(0)] Guid AgentGuid,
	[property: Key(1)] int AgentVersion,
	[property: Key(2)] string AuthToken
) : IMessageToServer {
	public Task Accept(IMessageToServerListener listener) {
		return listener.HandleAgentAuthentication(this);
	}
}
