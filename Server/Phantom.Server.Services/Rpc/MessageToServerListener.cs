using Phantom.Common.Data.Replies;
using Phantom.Common.Messages;
using Phantom.Common.Messages.ToAgent;
using Phantom.Common.Messages.ToServer;
using Phantom.Server.Rpc;
using Phantom.Server.Services.Agents;

namespace Phantom.Server.Services.Rpc;

public sealed class MessageToServerListener : IMessageToServerListener {
	private readonly RpcClientConnection connection;
	private readonly AgentManager agentManager;
	private Guid? agentGuid;

	public bool IsDisposed { get; private set; }

	public MessageToServerListener(RpcClientConnection connection, AgentManager agentManager) {
		this.connection = connection;
		this.agentManager = agentManager;
	}

	public async Task HandleRegisterAgent(RegisterAgentMessage message) {
		RegisterAgentResult result;

		if (agentGuid != null) {
			// TODO reconnection?
			result = RegisterAgentResult.DuplicateConnection;
		}
		else {
			result = await agentManager.RegisterAgent(message, connection);
		}

		if (result == RegisterAgentResult.Success) {
			agentGuid = message.AgentInfo.Guid;
		}

		await connection.Send(new RegisterAgentResultMessage(result));
	}

	public Task HandleUnregisterAgent(UnregisterAgentMessage message) {
		IsDisposed = true;
		agentManager.UnregisterAgent(message, connection);
		return Task.CompletedTask;
	}

	public Task HandleSimpleReply(SimpleReplyMessage message) {
		MessageReplyTracker.Instance.ReceiveReply(message);
		return Task.CompletedTask;
	}
}
