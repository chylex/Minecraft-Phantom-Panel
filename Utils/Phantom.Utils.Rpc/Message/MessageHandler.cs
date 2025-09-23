namespace Phantom.Utils.Rpc.Message;

sealed class MessageHandler<TMessageBase>(IMessageReceiver<TMessageBase> messageReceiver, IMessageReplySender replySender) {
	public IMessageReceiver<TMessageBase> Receiver => messageReceiver;
	
	public void OnPing() {
		messageReceiver.OnPing();
	}
	
	public ValueTask SendEmptyReply(uint messageId, CancellationToken cancellationToken) {
		return replySender.SendEmptyReply(messageId, cancellationToken);
	}
	
	public ValueTask SendReply<TReply>(uint messageId, TReply reply, CancellationToken cancellationToken) {
		return replySender.SendReply(messageId, reply, cancellationToken);
	}
	
	public ValueTask SendError(uint messageId, MessageError error, CancellationToken cancellationToken) {
		return replySender.SendError(messageId, error, cancellationToken);
	}
}
