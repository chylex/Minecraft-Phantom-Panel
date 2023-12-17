using NetMQ;
using NetMQ.Sockets;
using Phantom.Utils.Rpc.Message;

namespace Phantom.Utils.Rpc.Runtime;

public sealed class RpcConnectionToServer<TListener> : RpcConnection<TListener> {
	private readonly ClientSocket socket;

	internal RpcConnectionToServer(string loggerName, ClientSocket socket, MessageRegistry<TListener> messageRegistry, MessageReplyTracker replyTracker) : base(loggerName, messageRegistry, replyTracker) {
		this.socket = socket;
	}

	private protected override ValueTask Send(byte[] bytes) {
		return socket.SendAsync(bytes);
	}
}
