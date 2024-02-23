using Phantom.Common.Data.Replies;
using Phantom.Common.Messages.Agent;
using Phantom.Common.Messages.Agent.BiDirectional;
using Phantom.Common.Messages.Agent.ToAgent;
using Phantom.Common.Messages.Agent.ToController;
using Phantom.Controller.Services.Agents;
using Phantom.Controller.Services.Events;
using Phantom.Controller.Services.Instances;
using Phantom.Utils.Rpc.Message;
using Phantom.Utils.Rpc.Runtime;
using Phantom.Utils.Tasks;

namespace Phantom.Controller.Services.Rpc;

public sealed class AgentMessageListener : IMessageToControllerListener {
	private readonly RpcConnectionToClient<IMessageToAgentListener> connection;
	private readonly AgentManager agentManager;
	private readonly InstanceLogManager instanceLogManager;
	private readonly EventLogManager eventLogManager;
	private readonly CancellationToken cancellationToken;

	private readonly TaskCompletionSource<Guid> agentGuidWaiter = AsyncTasks.CreateCompletionSource<Guid>();

	internal AgentMessageListener(RpcConnectionToClient<IMessageToAgentListener> connection, AgentManager agentManager, InstanceLogManager instanceLogManager, EventLogManager eventLogManager, CancellationToken cancellationToken) {
		this.connection = connection;
		this.agentManager = agentManager;
		this.instanceLogManager = instanceLogManager;
		this.eventLogManager = eventLogManager;
		this.cancellationToken = cancellationToken;
	}

	public async Task<NoReply> HandleRegisterAgent(RegisterAgentMessage message) {
		if (agentGuidWaiter.Task.IsCompleted && agentGuidWaiter.Task.Result != message.AgentInfo.AgentGuid) {
			connection.SetAuthorizationResult(false);
			await connection.Send(new RegisterAgentFailureMessage(RegisterAgentFailure.ConnectionAlreadyHasAnAgent));
		}
		else if (await agentManager.RegisterAgent(message.AuthToken, message.AgentInfo, connection)) {
			connection.SetAuthorizationResult(true);
			agentGuidWaiter.SetResult(message.AgentInfo.AgentGuid);
		}
		
		return NoReply.Instance;
	}

	private async Task<Guid> WaitForAgentGuid() {
		return await agentGuidWaiter.Task.WaitAsync(cancellationToken);
	}
	
	public Task<NoReply> HandleUnregisterAgent(UnregisterAgentMessage message) {
		if (agentGuidWaiter.Task.IsCompleted) {
			agentManager.TellAgent(agentGuidWaiter.Task.Result, new AgentActor.UnregisterCommand(connection));
		}
		
		connection.Close();
		return Task.FromResult(NoReply.Instance);
	}

	public async Task<NoReply> HandleAgentIsAlive(AgentIsAliveMessage message) {
		agentManager.TellAgent(await WaitForAgentGuid(), new AgentActor.NotifyIsAliveCommand());
		return NoReply.Instance;
	}

	public async Task<NoReply> HandleAdvertiseJavaRuntimes(AdvertiseJavaRuntimesMessage message) {
		agentManager.TellAgent(await WaitForAgentGuid(), new AgentActor.UpdateJavaRuntimesCommand(message.Runtimes));
		return NoReply.Instance;
	}

	public async Task<NoReply> HandleReportAgentStatus(ReportAgentStatusMessage message) {
		agentManager.TellAgent(await WaitForAgentGuid(), new AgentActor.UpdateStatsCommand(message.RunningInstanceCount, message.RunningInstanceMemory));
		return NoReply.Instance;
	}

	public async Task<NoReply> HandleReportInstanceStatus(ReportInstanceStatusMessage message) {
		agentManager.TellAgent(await WaitForAgentGuid(), new AgentActor.UpdateInstanceStatusCommand(message.InstanceGuid, message.InstanceStatus));
		return NoReply.Instance;
	}

	public async Task<NoReply> HandleReportInstanceEvent(ReportInstanceEventMessage message) {
		message.Event.Accept(eventLogManager.CreateInstanceEventVisitor(message.EventGuid, message.UtcTime, await WaitForAgentGuid(), message.InstanceGuid));
		return NoReply.Instance;
	}

	public Task<NoReply> HandleInstanceOutput(InstanceOutputMessage message) {
		instanceLogManager.ReceiveLines(message.InstanceGuid, message.Lines);
		return Task.FromResult(NoReply.Instance);
	}

	public Task<NoReply> HandleReply(ReplyMessage message) {
		connection.Receive(message);
		return Task.FromResult(NoReply.Instance);
	}
}
