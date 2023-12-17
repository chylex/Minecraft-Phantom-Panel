using Phantom.Utils.Rpc.Message;

namespace Phantom.Common.Messages.Web;

public interface IMessageToWeb<TReply> : IMessage<IMessageToWebListener, TReply> {
	MessageQueueKey IMessage<IMessageToWebListener, TReply>.QueueKey => IMessageToWeb.DefaultQueueKey;
}

public interface IMessageToWeb : IMessageToWeb<NoReply> {
	internal static readonly MessageQueueKey DefaultQueueKey = new ("Web.Default");
	MessageQueueKey IMessage<IMessageToWebListener, NoReply>.QueueKey => DefaultQueueKey;
}
