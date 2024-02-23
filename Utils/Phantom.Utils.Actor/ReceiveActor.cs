using Akka.Actor;

namespace Phantom.Utils.Actor;

public abstract class ReceiveActor<TMessage> : ReceiveActor {
	protected ActorRef<TMessage> SelfTyped => new (Self);

	protected void ReceiveAndReply<TReplyableMessage, TReply>(Func<TReplyableMessage, TReply> action) where TReplyableMessage : TMessage, ICanReply<TReply> {
		Receive<TReplyableMessage>(message => HandleMessageWithReply(action, message));
	}

	protected void ReceiveAndReplyLater<TReplyableMessage, TReply>(Func<TReplyableMessage, Task<TReply>> action) where TReplyableMessage : TMessage, ICanReply<TReply> {
		// Must be async to set default task scheduler to actor scheduler.
		ReceiveAsync<TReplyableMessage>(message => HandleMessageWithReplyLater(action, message));
	}

	protected void ReceiveAsyncAndReply<TReplyableMessage, TReply>(Func<TReplyableMessage, Task<TReply>> action) where TReplyableMessage : TMessage, ICanReply<TReply> {
		ReceiveAsync<TReplyableMessage>(message => HandleMessageWithReplyAsync(action, message));
	}

	private void HandleMessageWithReply<TReplyableMessage, TReply>(Func<TReplyableMessage, TReply> action, TReplyableMessage message) where TReplyableMessage : TMessage, ICanReply<TReply> {
		try {
			Sender.Tell(action(message), Self);
		} catch (Exception e) {
			Sender.Tell(new Status.Failure(e), Self);
		}
	}

	private Task HandleMessageWithReplyLater<TReplyableMessage, TReply>(Func<TReplyableMessage, Task<TReply>> action, TReplyableMessage message) where TReplyableMessage : TMessage, ICanReply<TReply> {
		try {
			action(message).PipeTo(Sender, Self);
		} catch (Exception e) {
			Sender.Tell(new Status.Failure(e), Self);
		}
		
		return Task.CompletedTask;
	}

	private async Task HandleMessageWithReplyAsync<TReplyableMessage, TReply>(Func<TReplyableMessage, Task<TReply>> action, TReplyableMessage message) where TReplyableMessage : TMessage, ICanReply<TReply> {
		try {
			Sender.Tell(await action(message), Self);
		} catch (Exception e) {
			Sender.Tell(new Status.Failure(e), Self);
		}
	}
}
