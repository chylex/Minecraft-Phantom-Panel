using Phantom.Common.Data;
using Phantom.Common.Messages;
using Phantom.Server.Rpc;

namespace Phantom.Server.Services.Agents; 

internal sealed class AgentConnection {
	private readonly RpcClientConnection connection;

	public AgentInfo Info { get; }
	public bool IsClosed => connection.IsClosed;

	internal AgentConnection(RpcClientConnection connection, AgentInfo info) {
		this.connection = connection;
		this.Info = info;
	}

	public bool IsSame(RpcClientConnection connection) {
		return this.connection.IsSame(connection);
	}

	public async Task SendMessage<TMessage>(TMessage message) where TMessage : IMessageToAgent {
		await connection.Send(message);
	}
}
