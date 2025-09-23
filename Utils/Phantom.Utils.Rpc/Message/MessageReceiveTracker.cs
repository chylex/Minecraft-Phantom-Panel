using Phantom.Utils.Collections;

namespace Phantom.Utils.Rpc.Message;

sealed class MessageReceiveTracker {
	private readonly RangeSet<uint> receivedMessageIds = new ();
	
	public bool ReceiveMessage(uint messageId) {
		lock (receivedMessageIds) {
			return receivedMessageIds.Add(messageId);
		}
	}
}
