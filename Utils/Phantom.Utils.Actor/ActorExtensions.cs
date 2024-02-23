using Akka.Actor;

namespace Phantom.Utils.Actor;

public static class ActorExtensions {
	public static ActorRef<TMessage> ActorOf<TMessage>(this IActorRefFactory factory, Props<TMessage> props, string? name) {
		return new ActorRef<TMessage>(factory.ActorOf(props.Inner, name));
	}
}
