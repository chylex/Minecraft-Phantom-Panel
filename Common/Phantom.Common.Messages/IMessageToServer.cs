using Phantom.Utils.Rpc.Message;

namespace Phantom.Common.Messages; 

public interface IMessageToServer<TReply> : IMessage<IMessageToServerListener, TReply> {}

public interface IMessageToServer : IMessageToServer<NoReply> {
	uint IMessage<IMessageToServerListener, NoReply>.SequenceId => 0;
}
