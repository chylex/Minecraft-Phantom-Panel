using Phantom.Utils.Rpc.Message;

namespace Phantom.Common.Messages.Agent;

public interface IMessageToAgent<TReply> : IMessage<IMessageToAgentListener, TReply> {}

public interface IMessageToAgent : IMessageToAgent<NoReply> {}
