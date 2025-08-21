using Akka.Actor;
using Akka.Dispatch.MessageQueues;

namespace Phantom.Utils.Actor.Mailbox;

sealed class UnboundedJumpAheadMessageQueue : BlockingMessageQueue {
	private readonly Queue<Envelope> highPriorityQueue = new ();
	private readonly Queue<Envelope> lowPriorityQueue = new ();
	
	protected override int LockedCount => highPriorityQueue.Count + lowPriorityQueue.Count;
	
	protected override void LockedEnqueue(Envelope envelope) {
		if (envelope.Message is IJumpAhead) {
			highPriorityQueue.Enqueue(envelope);
		}
		else {
			lowPriorityQueue.Enqueue(envelope);
		}
	}
	
	protected override bool LockedTryDequeue(out Envelope envelope) {
		return highPriorityQueue.TryDequeue(out envelope) || lowPriorityQueue.TryDequeue(out envelope);
	}
}
