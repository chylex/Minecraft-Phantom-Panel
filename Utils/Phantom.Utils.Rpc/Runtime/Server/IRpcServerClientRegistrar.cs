using Phantom.Utils.Rpc.Message;

namespace Phantom.Utils.Rpc.Runtime.Server;

public interface IRpcServerClientRegistrar<TClientToServerMessage, TServerToClientMessage, THandshakeResult> {
	IMessageReceiver<TClientToServerMessage> Register(RpcServerToClientConnection<TClientToServerMessage, TServerToClientMessage> connection, THandshakeResult handshakeResult);
}
