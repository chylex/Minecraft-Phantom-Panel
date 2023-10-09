using NetMQ;
using NetMQ.Sockets;
using Phantom.Common.Messages;
using Phantom.Common.Messages.BiDirectional;
using Phantom.Utils.Rpc.Message;

namespace Phantom.Controller.Rpc;

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

	public async Task Send<TMessage>(TMessage message) where TMessage : IMessageToAgent {
		if (isClosed) {
			return;
		}
		
		var bytes = MessageRegistries.ToAgent.Write(message).ToArray();
		if (bytes.Length > 0) {
			await socket.SendAsync(routingId, bytes);
		}
	}

	public async Task<TReply?> Send<TMessage, TReply>(TMessage message, TimeSpan waitForReplyTime, CancellationToken waitForReplyCancellationToken) where TMessage : IMessageToAgent<TReply> where TReply : class {
		if (isClosed) {
			return null;
		}
		
		var sequenceId = messageReplyTracker.RegisterReply();
		
		var bytes = MessageRegistries.ToAgent.Write<TMessage, TReply>(sequenceId, message).ToArray();
		if (bytes.Length == 0) {
			messageReplyTracker.ForgetReply(sequenceId);
			return null;
		}

		await socket.SendAsync(routingId, bytes);
		return await messageReplyTracker.WaitForReply<TReply>(sequenceId, waitForReplyTime, waitForReplyCancellationToken);
	}

	public void Receive(ReplyMessage message) {
		messageReplyTracker.ReceiveReply(message.SequenceId, message.SerializedReply);
	}
}
