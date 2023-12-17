using Phantom.Utils.Logging;
using Serilog;

namespace Phantom.Utils.Rpc.Message;

abstract class MessageHandler<TListener> {
	protected ILogger Logger { get; }
	
	private readonly TListener listener;
	private readonly MessageQueues messageQueues;

	protected MessageHandler(string loggerName, TListener listener) {
		this.Logger = PhantomLogger.Create("MessageHandler", loggerName);
		this.listener = listener;
		this.messageQueues = new MessageQueues(loggerName + ":Receive");
	}
	
	internal void Enqueue<TMessage, TReply>(uint sequenceId, TMessage message) where TMessage : IMessage<TListener, TReply> {
		messageQueues.Enqueue(message.QueueKey, () => TryHandle<TMessage, TReply>(sequenceId, message));
	}

	private async Task TryHandle<TMessage, TReply>(uint sequenceId, TMessage message) where TMessage : IMessage<TListener, TReply> {
		TReply reply;
		try {
			reply = await message.Accept(listener);
		} catch (Exception e) {
			Logger.Error(e, "Failed to handle message {Type}.", message.GetType().Name);
			return;
		}
		
		if (reply is not NoReply) {
			await SendReply(sequenceId, MessageSerializer.Serialize(reply));
		}
	}

	protected abstract Task SendReply(uint sequenceId, byte[] serializedReply);

	internal Task StopReceiving() {
		return messageQueues.Stop();
	}
}
