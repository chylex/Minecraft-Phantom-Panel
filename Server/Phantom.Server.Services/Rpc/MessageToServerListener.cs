using Phantom.Common.Data.Replies;
using Phantom.Common.Messages;
using Phantom.Common.Messages.ToAgent;
using Phantom.Common.Messages.ToServer;
using Phantom.Server.Rpc;
using Phantom.Server.Services.Agents;
using Phantom.Server.Services.Instances;

namespace Phantom.Server.Services.Rpc;

public sealed class MessageToServerListener : IMessageToServerListener {
	private readonly RpcClientConnection connection;
	private readonly AgentManager agentManager;
	private readonly InstanceManager instanceManager;
	
	private Guid? agentGuid;

	public bool IsDisposed { get; private set; }

	public MessageToServerListener(RpcClientConnection connection, AgentManager agentManager, InstanceManager instanceManager) {
		this.connection = connection;
		this.agentManager = agentManager;
		this.instanceManager = instanceManager;
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

	public Task HandleInstanceOutput(InstanceOutputMessage message) {
		instanceManager.AddInstanceLogs(message);
		return Task.CompletedTask;
	}

	public Task HandleSimpleReply(SimpleReplyMessage message) {
		MessageReplyTracker.Instance.ReceiveReply(message);
		return Task.CompletedTask;
	}
}
