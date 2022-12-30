using NetMQ;
using NetMQ.Sockets;
using Phantom.Common.Messages;
using Phantom.Common.Messages.BiDirectional;
using Phantom.Utils.Rpc.Message;

namespace Phantom.Server.Rpc;

public sealed class RpcClientConnection {
	private readonly ServerSocket socket;
	private readonly uint routingId;

	private readonly MessageReplyTracker messageReplyTracker;

	internal event EventHandler<RpcClientConnectionClosedEventArgs>? Closed;
	private bool isClosed;

	internal RpcClientConnection(ServerSocket socket, uint routingId, MessageReplyTracker messageReplyTracker) {
		this.socket = socket;
		this.routingId = routingId;
		this.messageReplyTracker = messageReplyTracker;
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

	private byte[] WriteBytes<TMessage, TReply>(TMessage message) where TMessage : IMessageToAgent<TReply> {
		return isClosed ? Array.Empty<byte>() : MessageRegistries.ToAgent.Write<TMessage, TReply>(message).ToArray();
	}

	public async Task Send<TMessage>(TMessage message) where TMessage : IMessageToAgent {
		var bytes = WriteBytes<TMessage, NoReply>(message);
		if (bytes.Length > 0) {
			await socket.SendAsync(routingId, bytes);
		}
	}

	public async Task<TReply?> Send<TMessage, TReply>(Func<uint, TMessage> messageFactory, TimeSpan waitForReplyTime, CancellationToken cancellationToken) where TMessage : IMessageToAgent<TReply> where TReply : class {
		var sequenceId = messageReplyTracker.RegisterReply();
		var message = messageFactory(sequenceId);
		
		var bytes = WriteBytes<TMessage, TReply>(message);
		if (bytes.Length == 0) {
			messageReplyTracker.ForgetReply(sequenceId);
			return null;
		}

		await socket.SendAsync(routingId, bytes);
		return await messageReplyTracker.WaitForReply<TReply>(message.SequenceId, waitForReplyTime, cancellationToken);
	}

	public void Receive(ReplyMessage message) {
		messageReplyTracker.ReceiveReply(message.SequenceId, message.SerializedReply);
	}
}
