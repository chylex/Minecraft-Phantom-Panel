using Phantom.Utils.Runtime;
using Serilog;

namespace Phantom.Utils.Rpc.Message; 

public abstract class MessageHandler<TListener> {
	protected TListener Listener { get; }
	
	private readonly ILogger logger;
	private readonly TaskManager taskManager;
	private readonly CancellationToken cancellationToken;

	protected MessageHandler(TListener listener, ILogger logger, TaskManager taskManager, CancellationToken cancellationToken) {
		this.Listener = listener;
		this.logger = logger;
		this.taskManager = taskManager;
		this.cancellationToken = cancellationToken;
	}
	
	internal void Enqueue<TMessage, TReply>(uint sequenceId, TMessage message) where TMessage : IMessage<TListener, TReply> {
		cancellationToken.ThrowIfCancellationRequested();
		taskManager.Run("Handle message {Type}" + message.GetType().Name, async () => {
			try {
				await Handle<TMessage, TReply>(sequenceId, message);
			} catch (Exception e) {
				logger.Error(e, "Failed to handle message {Type}.", message.GetType().Name);
			}
		});
	}

	private async Task Handle<TMessage, TReply>(uint sequenceId, TMessage message) where TMessage : IMessage<TListener, TReply> {
		TReply reply = await message.Accept(Listener);
		
		if (reply is not NoReply) {
			await SendReply(sequenceId, MessageSerializer.Serialize(reply));
		}
	}
	
	protected abstract Task SendReply(uint sequenceId, byte[] serializedReply);
}
