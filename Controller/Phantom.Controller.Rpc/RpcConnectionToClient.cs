using NetMQ;
using NetMQ.Sockets;
using Phantom.Utils.Rpc.Message;

namespace Phantom.Controller.Rpc;

public sealed class RpcConnectionToClient<TListener> {
	private readonly ServerSocket socket;
	private readonly uint routingId;

	private readonly MessageRegistry<TListener> messageRegistry;
	private readonly MessageReplyTracker messageReplyTracker;

	private volatile bool isAuthorized;

	public bool IsAuthorized {
		get => isAuthorized;
		set => isAuthorized = value;
	}

	internal event EventHandler<RpcClientConnectionClosedEventArgs>? Closed;
	public bool IsClosed { get; private set; }

	internal RpcConnectionToClient(ServerSocket socket, uint routingId, MessageRegistry<TListener> messageRegistry, MessageReplyTracker messageReplyTracker) {
		this.socket = socket;
		this.routingId = routingId;
		this.messageRegistry = messageRegistry;
		this.messageReplyTracker = messageReplyTracker;
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

	public async Task Send<TMessage>(TMessage message) where TMessage : IMessage<TListener, NoReply> {
		if (IsClosed) {
			return;
		}
		
		var bytes = messageRegistry.Write(message).ToArray();
		if (bytes.Length > 0) {
			await socket.SendAsync(routingId, bytes);
		}
	}

	public async Task<TReply?> Send<TMessage, TReply>(TMessage message, TimeSpan waitForReplyTime, CancellationToken waitForReplyCancellationToken) where TMessage : IMessage<TListener, TReply> where TReply : class {
		if (IsClosed) {
			return null;
		}
		
		var sequenceId = messageReplyTracker.RegisterReply();
		
		var bytes = messageRegistry.Write<TMessage, TReply>(sequenceId, message).ToArray();
		if (bytes.Length == 0) {
			messageReplyTracker.ForgetReply(sequenceId);
			return null;
		}

		await socket.SendAsync(routingId, bytes);
		return await messageReplyTracker.TryWaitForReply<TReply>(sequenceId, waitForReplyTime, waitForReplyCancellationToken);
	}

	public void Receive(IReply message) {
		messageReplyTracker.ReceiveReply(message.SequenceId, message.SerializedReply);
	}
}
