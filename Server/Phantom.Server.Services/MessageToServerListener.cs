using Phantom.Common.Rpc.Messages;
using Phantom.Common.Rpc.Messages.ToAgent;
using Phantom.Common.Rpc.Messages.ToServer;
using Phantom.Server.Rpc;

namespace Phantom.Server.Services;

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
				result = RegisterAgentResultMessage.WithError("An agent is already registered on this connection.");
			}
			else if (!Services.AgentManager.RegisterAgent(message.AgentGuid, new AgentInfo(connection, message.AgentVersion))) {
				result = RegisterAgentResultMessage.WithError("Agent registration failed.");
			}
			else {
				agentGuid = message.AgentGuid;
				result = RegisterAgentResultMessage.WithSuccess;
			}
		}
		
		await connection.Send(result);
	}
}
