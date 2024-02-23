using Akka.Actor;

namespace Phantom.Utils.Actor;

public readonly struct ActorConfiguration {
	public SupervisorStrategy? SupervisorStrategy { get; init; }
	public string? MailboxType { get; init; }

	internal Props Apply(Props props) {
		if (SupervisorStrategy != null) {
			props = props.WithSupervisorStrategy(SupervisorStrategy);
		}
		
		if (MailboxType != null) {
			props = props.WithMailbox(MailboxType);
		}
		
		return props;
	}
}
