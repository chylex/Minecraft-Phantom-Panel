using Phantom.Common.Messages;
using Phantom.Server.Rpc;

namespace Phantom.Server.Services.Agents; 

sealed class AgentConnection {
	private readonly RpcClientConnection connection;

	internal AgentConnection(RpcClientConnection connection) {
		this.connection = connection;
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
