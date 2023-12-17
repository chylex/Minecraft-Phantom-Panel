using NetMQ;
using NetMQ.Sockets;
using Phantom.Utils.Rpc.Message;

namespace Phantom.Utils.Rpc.Runtime;

public sealed class RpcConnectionToServer<TListener> {
	private readonly ClientSocket socket;
	private readonly MessageRegistry<TListener> messageRegistry;
	private readonly MessageReplyTracker replyTracker;

	internal RpcConnectionToServer(ClientSocket socket, MessageRegistry<TListener> messageRegistry, MessageReplyTracker replyTracker) {
		this.socket = socket;
		this.messageRegistry = messageRegistry;
		this.replyTracker = replyTracker;
	}

	public async Task Send<TMessage>(TMessage message) where TMessage : IMessage<TListener, NoReply> {
		var bytes = messageRegistry.Write(message).ToArray();
		if (bytes.Length > 0) {
			await socket.SendAsync(bytes);
		}
	}

	public async Task<TReply?> TrySend<TMessage, TReply>(TMessage message, TimeSpan waitForReplyTime, CancellationToken waitForReplyCancellationToken) where TMessage : IMessage<TListener, TReply> where TReply : class {
		var sequenceId = replyTracker.RegisterReply();
		
		var bytes = messageRegistry.Write<TMessage, TReply>(sequenceId, message).ToArray();
		if (bytes.Length == 0) {
			replyTracker.ForgetReply(sequenceId);
			return null;
		}

		await socket.SendAsync(bytes);
		return await replyTracker.TryWaitForReply<TReply>(sequenceId, waitForReplyTime, waitForReplyCancellationToken);
	}
	
	public async Task<TReply> Send<TMessage, TReply>(TMessage message, TimeSpan waitForReplyTime, CancellationToken waitForReplyCancellationToken) where TMessage : IMessage<TListener, TReply> {
		var sequenceId = replyTracker.RegisterReply();
		
		var bytes = messageRegistry.Write<TMessage, TReply>(sequenceId, message).ToArray();
		if (bytes.Length == 0) {
			replyTracker.ForgetReply(sequenceId);
			throw new ArgumentException("Could not write message.", nameof(message));
		}

		await socket.SendAsync(bytes);
		return await replyTracker.WaitForReply<TReply>(sequenceId, waitForReplyTime, waitForReplyCancellationToken);
	}

	public void Receive(IReply message) {
		replyTracker.ReceiveReply(message.SequenceId, message.SerializedReply);
	}
}
