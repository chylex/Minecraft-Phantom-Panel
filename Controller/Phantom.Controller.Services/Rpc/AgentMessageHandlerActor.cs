using Phantom.Common.Data.Replies;
using Phantom.Common.Messages.Agent;
using Phantom.Common.Messages.Agent.BiDirectional;
using Phantom.Common.Messages.Agent.ToAgent;
using Phantom.Common.Messages.Agent.ToController;
using Phantom.Controller.Services.Agents;
using Phantom.Controller.Services.Events;
using Phantom.Controller.Services.Instances;
using Phantom.Utils.Actor;
using Phantom.Utils.Rpc.Runtime;

namespace Phantom.Controller.Services.Rpc;

sealed class AgentMessageHandlerActor : ReceiveActor<IMessageToController> {
	public readonly record struct Init(Guid AgentGuid, RpcConnectionToClient<IMessageToAgent> Connection, AgentRegistrationHandler AgentRegistrationHandler, AgentManager AgentManager, InstanceLogManager InstanceLogManager, EventLogManager EventLogManager);
	
	public static Props<IMessageToController> Factory(Init init) {
		return Props<IMessageToController>.Create(() => new AgentMessageHandlerActor(init), new ActorConfiguration { SupervisorStrategy = SupervisorStrategies.Resume });
	}

	private readonly Guid agentGuid;
	private readonly RpcConnectionToClient<IMessageToAgent> connection;
	private readonly AgentRegistrationHandler agentRegistrationHandler;
	private readonly AgentManager agentManager;
	private readonly InstanceLogManager instanceLogManager;
	private readonly EventLogManager eventLogManager;
	
	private AgentMessageHandlerActor(Init init) {
		this.agentGuid = init.AgentGuid;
		this.connection = init.Connection;
		this.agentRegistrationHandler = init.AgentRegistrationHandler;
		this.agentManager = init.AgentManager;
		this.instanceLogManager = init.InstanceLogManager;
		this.eventLogManager = init.EventLogManager;

		ReceiveAsync<RegisterAgentMessage>(HandleRegisterAgent);
		Receive<UnregisterAgentMessage>(HandleUnregisterAgent);
		Receive<AgentIsAliveMessage>(HandleAgentIsAlive);
		Receive<AdvertiseJavaRuntimesMessage>(HandleAdvertiseJavaRuntimes);
		Receive<ReportAgentStatusMessage>(HandleReportAgentStatus);
		Receive<ReportInstanceStatusMessage>(HandleReportInstanceStatus);
		Receive<ReportInstanceEventMessage>(HandleReportInstanceEvent);
		Receive<InstanceOutputMessage>(HandleInstanceOutput);
		Receive<ReplyMessage>(HandleReply);
	}

	private async Task HandleRegisterAgent(RegisterAgentMessage message) {
		if (agentGuid != message.AgentInfo.AgentGuid) {
			await connection.Send(new RegisterAgentFailureMessage(RegisterAgentFailure.ConnectionAlreadyHasAnAgent));
		}
		else {
			await agentRegistrationHandler.TryRegisterImpl(connection, message);
		}
	}

	private void HandleUnregisterAgent(UnregisterAgentMessage message) {
		agentManager.TellAgent(agentGuid, new AgentActor.UnregisterCommand(connection));
		connection.Close();
	}

	private void HandleAgentIsAlive(AgentIsAliveMessage message) {
		agentManager.TellAgent(agentGuid, new AgentActor.NotifyIsAliveCommand());
	}

	private void HandleAdvertiseJavaRuntimes(AdvertiseJavaRuntimesMessage message) {
		agentManager.TellAgent(agentGuid, new AgentActor.UpdateJavaRuntimesCommand(message.Runtimes));
	}

	private void HandleReportAgentStatus(ReportAgentStatusMessage message) {
		agentManager.TellAgent(agentGuid, new AgentActor.UpdateStatsCommand(message.RunningInstanceCount, message.RunningInstanceMemory));
	}

	private void HandleReportInstanceStatus(ReportInstanceStatusMessage message) {
		agentManager.TellAgent(agentGuid, new AgentActor.UpdateInstanceStatusCommand(message.InstanceGuid, message.InstanceStatus));
	}

	private void HandleReportInstanceEvent(ReportInstanceEventMessage message) {
		message.Event.Accept(eventLogManager.CreateInstanceEventVisitor(message.EventGuid, message.UtcTime, agentGuid, message.InstanceGuid));
	}

	private void HandleInstanceOutput(InstanceOutputMessage message) {
		instanceLogManager.ReceiveLines(message.InstanceGuid, message.Lines);
	}

	private void HandleReply(ReplyMessage message) {
		connection.Receive(message);
	}
}
