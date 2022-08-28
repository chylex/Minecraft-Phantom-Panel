using NetMQ;
using NetMQ.Sockets;
using Phantom.Common.Messages;

namespace Phantom.Server.Rpc;

public sealed class RpcClientConnection {
	private readonly ServerSocket socket;
	private readonly uint routingId;

	internal event EventHandler<RpcClientConnectionClosedEventArgs>? Closed;
	private bool isClosed;

	internal RpcClientConnection(ServerSocket socket, uint routingId) {
		this.socket = socket;
		this.routingId = routingId;
	}

	public bool IsSame(RpcClientConnection other) {
		return this.routingId == other.routingId;
	}

	public void Close() {
		lock (this) {
			if (!isClosed) {
				isClosed = true;
				Closed?.Invoke(this, new RpcClientConnectionClosedEventArgs(routingId));
			}
		}
	}

	public async Task Send<TMessage>(TMessage message) where TMessage : IMessageToAgent {
		if (isClosed) {
			return; // TODO
		}

		byte[] bytes = MessageRegistries.ToAgent.Write(message).ToArray();
		if (bytes.Length > 0) {
			await socket.SendAsync(routingId, bytes);
		}
	}
}
