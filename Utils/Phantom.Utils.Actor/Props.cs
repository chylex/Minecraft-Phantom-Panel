using Akka.Actor;

namespace Phantom.Utils.Actor;

public sealed class Props<TMessage> {
	internal Props Inner { get; }
	
	private Props(Props inner) {
		Inner = inner;
	}
	
	private static Props CreateInner<TActor>(Func<TActor> factory) where TActor : ReceiveActor<TMessage> {
		return Props.CreateBy(new ActorFactory<TActor>(factory));
	}
	
	public static Props<TMessage> Create<TActor>(Func<TActor> factory, ActorConfiguration configuration) where TActor : ReceiveActor<TMessage> {
		return new Props<TMessage>(configuration.Apply(CreateInner(factory)));
	}
}
