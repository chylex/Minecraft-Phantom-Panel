using Phantom.Common.Data.Agent;
using Phantom.Common.Messages;
using Phantom.Server.Rpc;

namespace Phantom.Server.Services.Agents; 

internal sealed class AgentConnection {
	private readonly RpcClientConnection connection;

	public AgentInfo Info { get; }

	internal AgentConnection(RpcClientConnection connection, AgentInfo info) {
		this.connection = connection;
		this.Info = info;
	}

	public bool IsSame(RpcClientConnection connection) {
		return this.connection.IsSame(connection);
	}

	public void Close() {
		connection.Close();
	}

	public async Task SendMessage<TMessage>(TMessage message) where TMessage : IMessageToAgent {
		await connection.Send(message);
	}
}
