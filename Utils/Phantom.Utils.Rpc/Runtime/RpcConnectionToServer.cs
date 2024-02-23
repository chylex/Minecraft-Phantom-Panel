using NetMQ;
using NetMQ.Sockets;
using Phantom.Utils.Rpc.Message;
using Phantom.Utils.Tasks;

namespace Phantom.Utils.Rpc.Runtime;

public sealed class RpcConnectionToServer<TListener> : RpcConnection<TListener> {
	private readonly ClientSocket socket;
	private readonly TaskCompletionSource isReady = AsyncTasks.CreateCompletionSource();

	public Task IsReady => isReady.Task;
	
	internal RpcConnectionToServer(string loggerName, ClientSocket socket, MessageRegistry<TListener> messageRegistry, MessageReplyTracker replyTracker) : base(loggerName, messageRegistry, replyTracker) {
		this.socket = socket;
	}

	public void SetIsReady() {
		isReady.TrySetResult();
	}

	private protected override ValueTask Send(byte[] bytes) {
		return socket.SendAsync(bytes);
	}
}
