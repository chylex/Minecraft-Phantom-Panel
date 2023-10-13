﻿using Phantom.Common.Messages.Web;
using Phantom.Utils.Rpc;

namespace Phantom.Web.Services.Rpc;

public sealed class ControllerConnection {
	private readonly RpcConnectionToServer<IMessageToControllerListener> connection;
	
	public ControllerConnection(RpcConnectionToServer<IMessageToControllerListener> connection) {
		this.connection = connection;
	}

	public Task Send<TMessage>(TMessage message) where TMessage : IMessageToController {
		return connection.Send(message);
	}

	public Task<TReply> Send<TMessage, TReply>(TMessage message, TimeSpan waitForReplyTime, CancellationToken waitForReplyCancellationToken = default) where TMessage : IMessageToController<TReply> {
		return connection.Send<TMessage, TReply>(message, waitForReplyTime, waitForReplyCancellationToken);
	}
	
	public Task<TReply> Send<TMessage, TReply>(TMessage message, CancellationToken waitForReplyCancellationToken) where TMessage : IMessageToController<TReply> {
		return connection.Send<TMessage, TReply>(message, Timeout.InfiniteTimeSpan, waitForReplyCancellationToken);
	}
}
