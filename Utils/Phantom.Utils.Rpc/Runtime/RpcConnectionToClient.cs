using NetMQ;
using NetMQ.Sockets;
using Phantom.Utils.Rpc.Message;

namespace Phantom.Utils.Rpc.Runtime;

public sealed class RpcConnectionToClient<TListener> : RpcConnection<TListener> {
	private readonly ServerSocket socket;
	private readonly uint routingId;

	private readonly TaskCompletionSource<bool> authorizationCompletionSource = new ();

	internal event EventHandler<RpcClientConnectionClosedEventArgs>? Closed;
	public bool IsClosed { get; private set; }

	internal RpcConnectionToClient(string loggerName, ServerSocket socket, uint routingId, MessageRegistry<TListener> messageRegistry, MessageReplyTracker replyTracker) : base(loggerName, messageRegistry, replyTracker) {
		this.socket = socket;
		this.routingId = routingId;
	}

	internal Task<bool> GetAuthorization() {
		return authorizationCompletionSource.Task;
	}
	
	public void SetAuthorizationResult(bool isAuthorized) {
		authorizationCompletionSource.SetResult(isAuthorized);
	}

	public bool IsSame(RpcConnectionToClient<TListener> other) {
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
