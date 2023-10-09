using Phantom.Common.Messages.Agent;
using Phantom.Controller.Rpc;

namespace Phantom.Controller.Services.Agents;

sealed class AgentConnection {
	private readonly RpcClientConnection<IMessageToAgentListener> connection;

	internal AgentConnection(RpcClientConnection<IMessageToAgentListener> connection) {
		this.connection = connection;
	}

	public bool IsSame(RpcClientConnection<IMessageToAgentListener> connection) {
		return this.connection.IsSame(connection);
	}

	public void Close() {
		connection.Close();
	}

	public Task Send<TMessage>(TMessage message) where TMessage : IMessageToAgent {
		return connection.Send(message);
	}

	public Task<TReply?> Send<TMessage, TReply>(TMessage message, TimeSpan waitForReplyTime, CancellationToken waitForReplyCancellationToken) where TMessage : IMessageToAgent<TReply> where TReply : class {
		return connection.Send<TMessage, TReply>(message, waitForReplyTime, waitForReplyCancellationToken);
	}
}
