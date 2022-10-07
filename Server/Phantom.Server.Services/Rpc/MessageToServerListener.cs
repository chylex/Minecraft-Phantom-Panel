using Phantom.Common.Data.Instance;
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
	private readonly CancellationToken cancellationToken;
	private readonly AgentManager agentManager;
	private readonly AgentJavaRuntimesManager agentJavaRuntimesManager;
	private readonly InstanceManager instanceManager;

	private Guid? agentGuid;
	private readonly TaskCompletionSource<Guid> agentGuidWaiter = new ();

	public bool IsDisposed { get; private set; }

	internal MessageToServerListener(RpcClientConnection connection, ServiceConfiguration configuration, AgentManager agentManager, AgentJavaRuntimesManager agentJavaRuntimesManager, InstanceManager instanceManager) {
		this.connection = connection;
		this.cancellationToken = configuration.CancellationToken;
		this.agentManager = agentManager;
		this.agentJavaRuntimesManager = agentJavaRuntimesManager;
		this.instanceManager = instanceManager;
	}

	public async Task HandleRegisterAgent(RegisterAgentMessage message) {
		if (agentGuid != null && agentGuid != message.AgentInfo.Guid) {
			await connection.Send(new RegisterAgentFailureMessage(RegisterAgentFailure.ConnectionAlreadyHasAnAgent));
		}
		else if (await agentManager.RegisterAgent(message.AuthToken, message.AgentInfo, instanceManager, connection)) {
			var guid = message.AgentInfo.Guid;
			agentGuid = guid;
			agentGuidWaiter.SetResult(guid);
		}
	}

	private async Task<Guid> WaitForAgentGuid() {
		return await agentGuidWaiter.Task.WaitAsync(cancellationToken);
	}
	
	public Task HandleUnregisterAgent(UnregisterAgentMessage message) {
		IsDisposed = true;
		
		if (agentManager.UnregisterAgent(message.AgentGuid, connection)) {
			instanceManager.SetInstanceStatesForAgent(message.AgentGuid, InstanceStatus.IsOffline);
		}

		return Task.CompletedTask;
	}

	public async Task HandleAgentIsAlive(AgentIsAliveMessage message) {
		agentManager.NotifyAgentIsAlive(await WaitForAgentGuid());
	}

	public async Task HandleAdvertiseJavaRuntimes(AdvertiseJavaRuntimesMessage message) {
		agentJavaRuntimesManager.Update(await WaitForAgentGuid(), message.Runtimes);
	}

	public Task HandleReportInstanceStatus(ReportInstanceStatusMessage message) {
		instanceManager.SetInstanceState(message.InstanceGuid, message.InstanceStatus);
		return Task.CompletedTask;
	}

	public Task HandleSimpleReply(SimpleReplyMessage message) {
		MessageReplyTracker.Instance.ReceiveReply(message);
		return Task.CompletedTask;
	}
}
