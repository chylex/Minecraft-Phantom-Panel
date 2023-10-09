using Phantom.Utils.Rpc.Message;

namespace Phantom.Common.Messages.Agent;

public interface IMessageToController<TReply> : IMessage<IMessageToControllerListener, TReply> {}

public interface IMessageToController : IMessageToController<NoReply> {}
