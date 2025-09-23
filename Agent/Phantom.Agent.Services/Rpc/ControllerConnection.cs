using Phantom.Common.Messages.Agent;
using Phantom.Utils.Actor;
using Phantom.Utils.Rpc.Message;

namespace Phantom.Agent.Services.Rpc;

public sealed class ControllerConnection(MessageSender<IMessageToController> sender) {
	internal bool TrySend<TMessage>(TMessage message) where TMessage : IMessageToController {
		return sender.TrySend(message);
	}
	
	internal ValueTask Send<TMessage>(TMessage message, CancellationToken cancellationToken) where TMessage : IMessageToController {
		return sender.Send(message, cancellationToken);
	}
	
	internal Task<TReply> Send<TMessage, TReply>(TMessage message, TimeSpan waitForReplyTime, CancellationToken cancellationToken) where TMessage : IMessageToController, ICanReply<TReply> {
		return sender.Send<TMessage, TReply>(message, waitForReplyTime, cancellationToken);
	}
}
