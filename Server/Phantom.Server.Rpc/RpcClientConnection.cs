using NetMQ;
using NetMQ.Sockets;
using Phantom.Common.Rpc.Message;
using Phantom.Common.Rpc.Messages;

namespace Phantom.Server.Rpc; 

public readonly struct RpcClientConnection {
	private readonly ServerSocket socket;
	private readonly uint routingId;

	internal RpcClientConnection(ServerSocket socket, uint routingId) {
		this.socket = socket;
		this.routingId = routingId;
	}

	public async Task Send(IMessageToAgent message) {
		byte[] bytes = MessageRegistries.ToAgent.Write(message).ToArray();
		await socket.SendAsync(routingId, bytes);
	}
}
