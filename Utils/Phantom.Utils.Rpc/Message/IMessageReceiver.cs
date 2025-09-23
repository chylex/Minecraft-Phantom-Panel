using Phantom.Utils.Actor;

namespace Phantom.Utils.Rpc.Message;

public interface IMessageReceiver<TMessageBase> {
	void OnSessionRestarted();
	Task OnSessionTerminated();
	
	void OnPing();
	void OnMessage(TMessageBase message);
	Task<TReply> OnMessage<TMessage, TReply>(TMessage message, CancellationToken cancellationToken = default) where TMessage : TMessageBase, ICanReply<TReply>;
	
	class Actor(ActorRef<TMessageBase> actor) : IMessageReceiver<TMessageBase> {
		public virtual void OnSessionRestarted() {}
		
		public virtual Task OnSessionTerminated() {
			return actor.Stop();
		}
		
		public virtual void OnPing() {}
		
		public void OnMessage(TMessageBase message) {
			actor.Tell(message);
		}
		
		public Task<TReply> OnMessage<TMessage, TReply>(TMessage message, CancellationToken cancellationToken = default) where TMessage : TMessageBase, ICanReply<TReply> {
			return actor.Request(message, cancellationToken);
		}
	}
}
