using Phantom.Common.Data;
using Phantom.Common.Messages;
using Phantom.Common.Messages.ToAgent;
using Phantom.Common.Messages.ToServer;
using Phantom.Server.Rpc;

namespace Phantom.Server.Services.Rpc;

public sealed class MessageToServerListener : IMessageToServerListener {
	private readonly RpcClientConnection connection;
	private Guid? agentGuid;
	
	public bool IsDisposed { get; private set; }

	public MessageToServerListener(RpcClientConnection connection) {
		this.connection = connection;
	}

	public async Task HandleRegisterAgent(RegisterAgentMessage message) {
		RegisterAgentResult result;
		
		lock (this) {
			if (agentGuid != null) {
				// TODO reconnection?
				result = RegisterAgentResult.DuplicateConnection;
			}
			else {
				result = Services.AgentManager.RegisterAgent(message, connection);
			}

			if (result == RegisterAgentResult.Success) {
				agentGuid = message.AgentInfo.Guid;
			}
		}

		await connection.Send(new RegisterAgentResultMessage(result));
	}

	public Task HandleUnregisterAgent(UnregisterAgentMessage message) {
		IsDisposed = true;
		Services.AgentManager.UnregisterAgent(message, connection);
		return Task.CompletedTask;
	}

	public Task HandleSimpleReply(SimpleReplyMessage message) {
		Services.MessageReplyTracker.ReceiveReply(message);
		return Task.CompletedTask;
	}
}
