using Akka.Actor;

namespace Phantom.Utils.Actor;

public readonly struct ActorRef<TMessage> {
	private readonly IActorRef actorRef;
	
	internal ActorRef(IActorRef actorRef) {
		this.actorRef = actorRef;
	}
	
	internal bool IsSame<TOtherMessage>(ActorRef<TOtherMessage> other) {
		return actorRef.Equals(other.actorRef);
	}
	
	public void Tell(TMessage message) {
		actorRef.Tell(message);
	}
	
	public void Forward(TMessage message) {
		actorRef.Forward(message);
	}
	
	public Task<TReply> Request<TReply>(ICanReply<TReply> message, TimeSpan? timeout, CancellationToken cancellationToken = default) {
		return actorRef.Ask<TReply>(message, timeout, cancellationToken);
	}
	
	public Task<TReply> Request<TReply>(ICanReply<TReply> message, CancellationToken cancellationToken = default) {
		return Request(message, timeout: null, cancellationToken);
	}
	
	public Task<bool> Stop(TMessage message, TimeSpan? timeout = null) {
		return actorRef.GracefulStop(timeout ?? Timeout.InfiniteTimeSpan, message);
	}
	
	public Task<bool> Stop(TimeSpan? timeout = null) {
		return actorRef.GracefulStop(timeout ?? Timeout.InfiniteTimeSpan);
	}
}
