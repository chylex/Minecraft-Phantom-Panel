using Akka.Actor;

namespace Phantom.Utils.Actor;

sealed class ActorFactory<TActor>(Func<TActor> constructor) : IIndirectActorProducer where TActor : ActorBase {
	public Type ActorType => typeof(TActor);
	
	public ActorBase Produce() {
		return constructor();
	}
	
	public void Release(ActorBase actor) {}
}
