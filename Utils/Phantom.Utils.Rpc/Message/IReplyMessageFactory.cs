namespace Phantom.Utils.Rpc.Message;

public interface IReplyMessageFactory<TReplyMessage> {
	TReplyMessage CreateReplyMessage(uint sequenceId, byte[] serializedReply);
}
