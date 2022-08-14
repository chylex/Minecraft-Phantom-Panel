using MessagePack;
using Phantom.Common.Rpc.Message;

namespace Phantom.Common.Rpc.Messages.ToServer;

[MessagePackObject]
public sealed record RegisterAgentMessage(
	[property: Key(0)] string AuthToken,
	[property: Key(1)] Guid AgentGuid,
	[property: Key(2)] int AgentVersion,
	[property: Key(3)] string AgentName
) : IMessageToServer {
	public Task Accept(IMessageToServerListener listener) {
		return listener.HandleAgentAuthentication(this);
	}
}
