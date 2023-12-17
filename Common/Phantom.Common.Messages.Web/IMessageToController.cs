using Phantom.Utils.Rpc.Message;

namespace Phantom.Common.Messages.Web;

public interface IMessageToController<TReply> : IMessage<IMessageToControllerListener, TReply> {
	MessageQueueKey IMessage<IMessageToControllerListener, TReply>.QueueKey => IMessageToController.DefaultQueueKey;
}

public interface IMessageToController : IMessageToController<NoReply> {
	internal static readonly MessageQueueKey DefaultQueueKey = new ("Web.Default");
	MessageQueueKey IMessage<IMessageToControllerListener, NoReply>.QueueKey => DefaultQueueKey;
}
