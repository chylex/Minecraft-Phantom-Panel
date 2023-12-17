using Phantom.Utils.Rpc.Message;

namespace Phantom.Utils.Rpc.Runtime; 

public abstract class RpcConnection<TListener>  {
	private readonly MessageRegistry<TListener> messageRegistry;
	private readonly MessageQueues sendingQueues;
	private readonly MessageReplyTracker replyTracker;
	
	internal RpcConnection(string loggerName, MessageRegistry<TListener> messageRegistry, MessageReplyTracker replyTracker) {
		this.messageRegistry = messageRegistry;
		this.sendingQueues = new MessageQueues(loggerName + ":Send");
		this.replyTracker = replyTracker;
	}

	private protected abstract ValueTask Send(byte[] bytes);

	public Task Send<TMessage>(TMessage message) where TMessage : IMessage<TListener, NoReply> {
		return sendingQueues.Enqueue(message.QueueKey, () => SendTask(message));
	}

	public Task<TReply> Send<TMessage, TReply>(TMessage message, TimeSpan waitForReplyTime, CancellationToken waitForReplyCancellationToken) where TMessage : IMessage<TListener, TReply> {
		return sendingQueues.Enqueue(message.QueueKey, () => SendTask<TMessage, TReply>(message, waitForReplyTime, waitForReplyCancellationToken));
	}

	private async Task SendTask<TMessage>(TMessage message) where TMessage : IMessage<TListener, NoReply> {
		var bytes = messageRegistry.Write(message).ToArray();
		if (bytes.Length > 0) {
			await Send(bytes);
		}
	}

	private async Task<TReply> SendTask<TMessage, TReply>(TMessage message, TimeSpan waitForReplyTime, CancellationToken waitForReplyCancellationToken) where TMessage : IMessage<TListener, TReply> {
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

	internal Task StopSending() {
		return sendingQueues.Stop();
	}
}
