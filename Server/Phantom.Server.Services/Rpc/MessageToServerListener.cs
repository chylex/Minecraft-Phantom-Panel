using Phantom.Common.Rpc.Messages;
using Phantom.Common.Rpc.Messages.ToAgent;
using Phantom.Common.Rpc.Messages.ToServer;
using Phantom.Server.Rpc;

namespace Phantom.Server.Services.Rpc;

public sealed class MessageToServerListener : IMessageToServerListener {
	private readonly RpcClientConnection connection;
	private Guid? agentGuid;

	public MessageToServerListener(RpcClientConnection connection) {
		this.connection = connection;
	}

	public async Task HandleAgentAuthentication(RegisterAgentMessage message) {
		RegisterAgentResultMessage result;
		
		lock (this) {
			if (agentGuid != null) {
				// TODO reconnection?
				result = RegisterAgentResultMessage.WithError("This connection already has an associated agent.");
			}
			else {
				result = Services.AgentManager.RegisterAgent(message, connection);
			}

			if (result.Success) {
				agentGuid = message.AgentGuid;
			}
		}

		await connection.Send(result);
	}
}
