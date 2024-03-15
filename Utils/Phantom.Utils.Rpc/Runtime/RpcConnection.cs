using Phantom.Utils.Actor;
using Phantom.Utils.Rpc.Message;

namespace Phantom.Utils.Rpc.Runtime; 

public abstract class RpcConnection<TMessageBase> {
	private readonly MessageRegistry<TMessageBase> messageRegistry;
	private readonly MessageReplyTracker replyTracker;
	
	internal RpcConnection(MessageRegistry<TMessageBase> messageRegistry, MessageReplyTracker replyTracker) {
		this.messageRegistry = messageRegistry;
		this.replyTracker = replyTracker;
	}

	private protected abstract ValueTask Send(byte[] bytes);

	public async Task Send<TMessage>(TMessage message) where TMessage : TMessageBase {
		var bytes = messageRegistry.Write(message).ToArray();
		if (bytes.Length > 0) {
			await Send(bytes);
		}
	}

	public async Task<TReply> Send<TMessage, TReply>(TMessage message, TimeSpan waitForReplyTime, CancellationToken waitForReplyCancellationToken) where TMessage : TMessageBase, ICanReply<TReply> {
		var sequenceId = replyTracker.RegisterReply();
		
		var bytes = messageRegistry.Write<TMessage, TReply>(sequenceId, message).ToArray();
		if (bytes.Length == 0) {
			replyTracker.ForgetReply(sequenceId);
			throw new ArgumentException("Could not write message.", nameof(message));
		}

		await Send(bytes);
		return await replyTracker.WaitForReply<TReply>(sequenceId, waitForReplyTime, waitForReplyCancellationToken);
	}

	public void Receive(IReply message) {
		replyTracker.ReceiveReply(message.SequenceId, message.SerializedReply);
	}
}
