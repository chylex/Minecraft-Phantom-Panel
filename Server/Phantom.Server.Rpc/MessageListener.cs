using NetMQ;
using NetMQ.Sockets;
using Phantom.Common.Rpc.Messages;
using Phantom.Common.Rpc.Messages.ToAgent;
using Phantom.Common.Rpc.Messages.ToServer;

namespace Phantom.Server.Rpc; 

sealed class MessageListener : IMessageToServerListener {
	private readonly ServerSocket socket;
	private readonly uint routingId;

	public MessageListener(ServerSocket socket, uint routingId) {
		this.socket = socket;
		this.routingId = routingId;
	}

	public async Task HandleAgentAuthentication(AgentAuthenticationMessage message) {
		byte[] bytes = MessageRegistries.ToAgent.Write(new AgentAuthenticationResultMessage(Success: true, ErrorMessage: null)).ToArray();
		await socket.SendAsync(routingId, bytes);
	}
}
