using Phantom.Utils.Rpc.Message;

namespace Phantom.Utils.Rpc.Runtime.Server;

public interface IRpcServerClientRegistrar<TClientToServerMessage, TServerToClientMessage> {
	IMessageReceiver<TClientToServerMessage> Register(RpcServerToClientConnection<TClientToServerMessage, TServerToClientMessage> connection);
}
