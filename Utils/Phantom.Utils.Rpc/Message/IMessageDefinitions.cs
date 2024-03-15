namespace Phantom.Utils.Rpc.Message;

public interface IMessageDefinitions<TClientMessage, TServerMessage, TReplyMessage> : IReplyMessageFactory<TReplyMessage> where TReplyMessage : TClientMessage, TServerMessage {
	MessageRegistry<TClientMessage> ToClient { get; }
	MessageRegistry<TServerMessage> ToServer { get; }
}
