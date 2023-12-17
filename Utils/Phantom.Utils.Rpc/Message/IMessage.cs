namespace Phantom.Utils.Rpc.Message;

public interface IMessage<TListener, TReply> {
	MessageQueueKey QueueKey { get; }
	Task<TReply> Accept(TListener listener);
}
