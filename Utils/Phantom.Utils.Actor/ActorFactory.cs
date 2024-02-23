using Akka.Actor;

namespace Phantom.Utils.Actor;

sealed class ActorFactory<TActor> : IIndirectActorProducer where TActor : ActorBase {
	public Type ActorType => typeof(TActor);
		
	private readonly Func<TActor> constructor;
		
	public ActorFactory(Func<TActor> constructor) {
		this.constructor = constructor;
	}

	public ActorBase Produce() {
		return constructor();
	}

	public void Release(ActorBase actor) {}
}
