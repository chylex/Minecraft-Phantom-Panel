using Phantom.Common.Rpc.Messages;

namespace Phantom.Common.Rpc.Message; 

public interface IMessageToServer : IMessage<IMessageToServerListener> {}
