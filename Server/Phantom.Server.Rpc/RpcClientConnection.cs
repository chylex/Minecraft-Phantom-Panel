using NetMQ;
using NetMQ.Sockets;
using Phantom.Common.Messages;

namespace Phantom.Server.Rpc; 

public sealed class RpcClientConnection {
	private readonly ServerSocket socket;
	private readonly uint routingId;

	public bool IsClosed { get; internal set; }

	internal RpcClientConnection(ServerSocket socket, uint routingId) {
		this.socket = socket;
		this.routingId = routingId;
	}

	public bool IsSame(RpcClientConnection other) {
		return this.routingId == other.routingId;
	}
	
	public async Task Send<TMessage>(TMessage message) where TMessage : IMessageToAgent {
		if (IsClosed) {
			return; // TODO
		}
		
		byte[] bytes = MessageRegistries.ToAgent.Write(message).ToArray();
		if (bytes.Length > 0) {
			await socket.SendAsync(routingId, bytes);
		}
	}
}
