using NetMQ;
using NetMQ.Sockets;
using Phantom.Utils.Rpc.Message;

namespace Phantom.Utils.Rpc.Runtime;

public sealed class RpcConnectionToClient<TMessageBase> : RpcConnection<TMessageBase> {
	private readonly ServerSocket socket;
	private readonly uint routingId;

	internal event EventHandler<RpcClientConnectionClosedEventArgs>? Closed;
	public bool IsClosed { get; private set; }

	internal RpcConnectionToClient(ServerSocket socket, uint routingId, MessageRegistry<TMessageBase> messageRegistry, MessageReplyTracker replyTracker) : base(messageRegistry, replyTracker) {
		this.socket = socket;
		this.routingId = routingId;
	}

	public bool IsSame(RpcConnectionToClient<TMessageBase> other) {
		return this.routingId == other.routingId && this.socket == other.socket;
	}

	public void Close() {
		bool hasClosed = false;
		
		lock (this) {
			if (!IsClosed) {
				IsClosed = true;
				hasClosed = true;
			}
		}

		if (hasClosed) {
			Closed?.Invoke(this, new RpcClientConnectionClosedEventArgs(routingId));
		}
	}

	private protected override ValueTask Send(byte[] bytes) {
		return socket.SendAsync(routingId, bytes);
	}
}
