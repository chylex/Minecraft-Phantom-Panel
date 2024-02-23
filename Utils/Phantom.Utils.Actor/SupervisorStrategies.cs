using Akka.Actor;
using Akka.Util.Internal;

namespace Phantom.Utils.Actor;

public static class SupervisorStrategies {
	private static DeployableDecider DefaultDecider { get; } = SupervisorStrategy.DefaultDecider.AsInstanceOf<DeployableDecider>();
	
	public static SupervisorStrategy Resume { get; } = new OneForOneStrategy(Decider.From(Directive.Resume, DefaultDecider.Pairs));
}
