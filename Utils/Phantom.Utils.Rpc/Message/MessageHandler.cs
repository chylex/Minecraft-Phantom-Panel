using Phantom.Utils.Actor;
using Phantom.Utils.Logging;
using Serilog;

namespace Phantom.Utils.Rpc.Message;

sealed class MessageHandler<TMessageBase> {
	private readonly ILogger logger;
	private readonly ActorRef<TMessageBase> handlerActor;
	private readonly IReplySender replySender;
	
	public MessageHandler(string loggerName, ActorRef<TMessageBase> handlerActor, IReplySender replySender) {
		this.logger = PhantomLogger.Create("MessageHandler", loggerName);
		this.handlerActor = handlerActor;
		this.replySender = replySender;
	}

	public void Tell(TMessageBase message) {
		handlerActor.Tell(message);
	}
	
	public Task TellAndReply<TMessage, TReply>(TMessage message, uint sequenceId) where TMessage : ICanReply<TReply> {
		return handlerActor.Request(message).ContinueWith(task => {
			if (task.IsCompletedSuccessfully) {
				return replySender.SendReply(sequenceId, MessageSerializer.Serialize(task.Result));
			}
			
			if (task.IsFaulted) {
				logger.Error(task.Exception, "Failed to handle message {Type}.", message.GetType().Name);
			}
			
			return task;
		}, TaskScheduler.Default);
	}
}
