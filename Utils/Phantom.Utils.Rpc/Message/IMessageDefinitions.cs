namespace Phantom.Utils.Rpc.Message;

public interface IMessageDefinitions<TOutgoingListener, TIncomingListener, TReplyMessage> where TReplyMessage : IMessage<TOutgoingListener, NoReply>, IMessage<TIncomingListener, NoReply> {
	MessageRegistry<TOutgoingListener> Outgoing { get; }
	MessageRegistry<TIncomingListener> Incoming { get; }
	
	bool IsRegistrationMessage(Type messageType);
	TReplyMessage CreateReplyMessage(uint sequenceId, byte[] serializedReply);
}
