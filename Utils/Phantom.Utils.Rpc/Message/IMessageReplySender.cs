namespace Phantom.Utils.Rpc.Message;

interface IMessageReplySender {
	ValueTask SendEmptyReply(uint replyingToMessageId, CancellationToken cancellationToken);
	ValueTask SendReply<TReply>(uint replyingToMessageId, TReply reply, CancellationToken cancellationToken);
	ValueTask SendError(uint replyingToMessageId, MessageError error, CancellationToken cancellationToken);
}
