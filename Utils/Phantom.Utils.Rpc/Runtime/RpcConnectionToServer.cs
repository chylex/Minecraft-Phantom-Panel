using NetMQ;
using NetMQ.Sockets;
using Phantom.Utils.Rpc.Message;
using Phantom.Utils.Tasks;

namespace Phantom.Utils.Rpc.Runtime;

public sealed class RpcConnectionToServer<TMessageBase> : RpcConnection<TMessageBase> {
	private readonly ClientSocket socket;
	private readonly TaskCompletionSource isReady = AsyncTasks.CreateCompletionSource();

	public Task IsReady => isReady.Task;
	
	internal RpcConnectionToServer(ClientSocket socket, MessageRegistry<TMessageBase> messageRegistry, MessageReplyTracker replyTracker) : base(messageRegistry, replyTracker) {
		this.socket = socket;
	}

	public void SetIsReady() {
		isReady.TrySetResult();
	}

	private protected override ValueTask Send(byte[] bytes) {
		return socket.SendAsync(bytes);
	}
}
