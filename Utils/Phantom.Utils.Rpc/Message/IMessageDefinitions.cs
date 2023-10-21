namespace Phantom.Utils.Rpc.Message;

public interface IMessageDefinitions<TClientListener, TServerListener, TReplyMessage> where TReplyMessage : IMessage<TClientListener, NoReply>, IMessage<TServerListener, NoReply> {
	MessageRegistry<TClientListener> ToClient { get; }
	MessageRegistry<TServerListener> ToServer { get; }
	
	bool IsRegistrationMessage(Type messageType);
	TReplyMessage CreateReplyMessage(uint sequenceId, byte[] serializedReply);
}
