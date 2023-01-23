using Phantom.Utils.Rpc.Message;

namespace Phantom.Common.Messages;

public interface IMessageToAgent<TReply> : IMessage<IMessageToAgentListener, TReply> {}

public interface IMessageToAgent : IMessageToAgent<NoReply> {}
