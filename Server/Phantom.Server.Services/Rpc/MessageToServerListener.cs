using Phantom.Common.Data.Instance;
using Phantom.Common.Data.Replies;
using Phantom.Common.Messages;
using Phantom.Common.Messages.BiDirectional;
using Phantom.Common.Messages.ToAgent;
using Phantom.Common.Messages.ToServer;
using Phantom.Server.Rpc;
using Phantom.Server.Services.Agents;
using Phantom.Server.Services.Events;
using Phantom.Server.Services.Instances;
using Phantom.Utils.Rpc.Message;

namespace Phantom.Server.Services.Rpc;

public sealed class MessageToServerListener : IMessageToServerListener {
	private readonly RpcClientConnection connection;
	private readonly CancellationToken cancellationToken;
	private readonly AgentManager agentManager;
	private readonly AgentJavaRuntimesManager agentJavaRuntimesManager;
	private readonly InstanceManager instanceManager;
	private readonly InstanceLogManager instanceLogManager;
	private readonly EventLog eventLog;

	private readonly TaskCompletionSource<Guid> agentGuidWaiter = new ();

	public bool IsDisposed { get; private set; }

	internal MessageToServerListener(RpcClientConnection connection, ServiceConfiguration configuration, AgentManager agentManager, AgentJavaRuntimesManager agentJavaRuntimesManager, InstanceManager instanceManager, InstanceLogManager instanceLogManager, EventLog eventLog) {
		this.connection = connection;
		this.cancellationToken = configuration.CancellationToken;
		this.agentManager = agentManager;
		this.agentJavaRuntimesManager = agentJavaRuntimesManager;
		this.instanceManager = instanceManager;
		this.instanceLogManager = instanceLogManager;
		this.eventLog = eventLog;
	}

	public async Task<NoReply> HandleRegisterAgent(RegisterAgentMessage message) {
		if (agentGuidWaiter.Task.IsCompleted && agentGuidWaiter.Task.Result != message.AgentInfo.Guid) {
			await connection.Send(new RegisterAgentFailureMessage(RegisterAgentFailure.ConnectionAlreadyHasAnAgent));
		}
		else if (await agentManager.RegisterAgent(message.AuthToken, message.AgentInfo, instanceManager, connection)) {
			var guid = message.AgentInfo.Guid;
			agentGuidWaiter.SetResult(guid);
		}
		
		return NoReply.Instance;
	}

	private async Task<Guid> WaitForAgentGuid() {
		return await agentGuidWaiter.Task.WaitAsync(cancellationToken);
	}
	
	public Task<NoReply> HandleUnregisterAgent(UnregisterAgentMessage message) {
		IsDisposed = true;
		
		if (agentManager.UnregisterAgent(message.AgentGuid, connection)) {
			instanceManager.SetInstanceStatesForAgent(message.AgentGuid, InstanceStatus.Offline);
		}

		return Task.FromResult(NoReply.Instance);
	}

	public async Task<NoReply> HandleAgentIsAlive(AgentIsAliveMessage message) {
		agentManager.NotifyAgentIsAlive(await WaitForAgentGuid());
		return NoReply.Instance;
	}

	public async Task<NoReply> HandleAdvertiseJavaRuntimes(AdvertiseJavaRuntimesMessage message) {
		agentJavaRuntimesManager.Update(await WaitForAgentGuid(), message.Runtimes);
		return NoReply.Instance;
	}

	public async Task<NoReply> HandleReportAgentStatus(ReportAgentStatusMessage message) {
		agentManager.SetAgentStats(await WaitForAgentGuid(), message.RunningInstanceCount, message.RunningInstanceMemory);
		return NoReply.Instance;
	}

	public Task<NoReply> HandleReportInstanceStatus(ReportInstanceStatusMessage message) {
		instanceManager.SetInstanceState(message.InstanceGuid, message.InstanceStatus);
		return Task.FromResult(NoReply.Instance);
	}

	public async Task<NoReply> HandleReportInstanceEvent(ReportInstanceEventMessage message) {
		message.Event.Accept(eventLog.CreateInstanceEventVisitor(message.EventGuid, message.UtcTime, await WaitForAgentGuid(), message.InstanceGuid));
		return NoReply.Instance;
	}

	public Task<NoReply> HandleInstanceOutput(InstanceOutputMessage message) {
		instanceLogManager.AddLines(message.InstanceGuid, message.Lines);
		return Task.FromResult(NoReply.Instance);
	}

	public Task<NoReply> HandleReply(ReplyMessage message) {
		connection.Receive(message);
		return Task.FromResult(NoReply.Instance);
	}
}
