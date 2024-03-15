using Phantom.Utils.Actor;

namespace Phantom.Utils.Rpc.Runtime;

public interface IRegistrationHandler<TClientMessage, TServerMessage, TRegistrationMessage> where TRegistrationMessage : TServerMessage {
	Task<Props<TServerMessage>?> TryRegister(RpcConnectionToClient<TClientMessage> connection, TRegistrationMessage message);
}
