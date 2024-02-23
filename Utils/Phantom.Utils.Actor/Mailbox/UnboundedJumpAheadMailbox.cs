using Akka.Actor;
using Akka.Configuration;
using Akka.Dispatch;
using Akka.Dispatch.MessageQueues;

namespace Phantom.Utils.Actor.Mailbox;

public sealed class UnboundedJumpAheadMailbox : MailboxType, IProducesMessageQueue<UnboundedJumpAheadMessageQueue> {
	public const string Name = "unbounded-jump-ahead-mailbox";
	
	public UnboundedJumpAheadMailbox(Settings settings, Config config) : base(settings, config) {}
	
	public override IMessageQueue Create(IActorRef owner, ActorSystem system) {
		return new UnboundedJumpAheadMessageQueue();
	}
}
