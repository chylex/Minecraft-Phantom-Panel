using Phantom.Common.Messages.Web;
using Phantom.Utils.Actor;
using Phantom.Utils.Rpc.Message;

namespace Phantom.Web.Services.Rpc;

public sealed class ControllerConnection(MessageSender<IMessageToController> sender) {
	public ValueTask Send<TMessage>(TMessage message) where TMessage : IMessageToController {
		return sender.Send(message);
	}
	
	public Task<TReply> Send<TMessage, TReply>(TMessage message, TimeSpan waitForReplyTime, CancellationToken waitForReplyCancellationToken = default) where TMessage : IMessageToController, ICanReply<TReply> {
		return sender.Send<TMessage, TReply>(message, waitForReplyTime, waitForReplyCancellationToken);
	}
	
	public Task<TReply> Send<TMessage, TReply>(TMessage message, CancellationToken waitForReplyCancellationToken) where TMessage : IMessageToController, ICanReply<TReply> {
		return sender.Send<TMessage, TReply>(message, Timeout.InfiniteTimeSpan, waitForReplyCancellationToken);
	}
}
