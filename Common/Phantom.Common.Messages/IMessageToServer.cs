using Phantom.Utils.Rpc.Message;

namespace Phantom.Common.Messages; 

public interface IMessageToServer : IMessage<IMessageToServerListener> {}
