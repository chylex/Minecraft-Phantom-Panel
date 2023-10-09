using Phantom.Utils.Rpc.Message;

namespace Phantom.Common.Messages.Web;

public interface IMessageToController<TReply> : IMessage<IMessageToControllerListener, TReply> {}

public interface IMessageToController : IMessageToController<NoReply> {}
