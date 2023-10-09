using Phantom.Utils.Rpc.Message;

namespace Phantom.Common.Messages.Web;

public interface IMessageToWeb<TReply> : IMessage<IMessageToWebListener, TReply> {}

public interface IMessageToWeb : IMessageToWeb<NoReply> {}
