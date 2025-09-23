namespace Phantom.Utils.Rpc.Message;

public interface IMessageDefinitions<TClientToServerMessage, TServerToClientMessage> {
	MessageRegistry<TServerToClientMessage> ToClient { get; }
	MessageRegistry<TClientToServerMessage> ToServer { get; }
}
