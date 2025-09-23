using Phantom.Common.Messages.Agent;
using Phantom.Common.Messages.Agent.ToController;
using Phantom.Controller.Services.Agents;
using Phantom.Controller.Services.Events;
using Phantom.Controller.Services.Instances;
using Phantom.Utils.Actor;

namespace Phantom.Controller.Services.Rpc;

sealed class AgentMessageHandlerActor : ReceiveActor<IMessageToController> {
	public readonly record struct Init(Guid AgentGuid, AgentManager AgentManager, InstanceLogManager InstanceLogManager, EventLogManager EventLogManager);
	
	public static Props<IMessageToController> Factory(Init init) {
		return Props<IMessageToController>.Create(() => new AgentMessageHandlerActor(init), new ActorConfiguration { SupervisorStrategy = SupervisorStrategies.Resume });
	}
	
	private readonly Guid agentGuid;
	private readonly AgentManager agentManager;
	private readonly InstanceLogManager instanceLogManager;
	private readonly EventLogManager eventLogManager;
	
	private AgentMessageHandlerActor(Init init) {
		this.agentGuid = init.AgentGuid;
		this.agentManager = init.AgentManager;
		this.instanceLogManager = init.InstanceLogManager;
		this.eventLogManager = init.EventLogManager;
		
		Receive<ReportAgentStatusMessage>(HandleReportAgentStatus);
		Receive<ReportInstanceStatusMessage>(HandleReportInstanceStatus);
		Receive<ReportInstancePlayerCountsMessage>(HandleReportInstancePlayerCounts);
		Receive<ReportInstanceEventMessage>(HandleReportInstanceEvent);
		Receive<InstanceOutputMessage>(HandleInstanceOutput);
	}
	
	private void HandleReportAgentStatus(ReportAgentStatusMessage message) {
		agentManager.TellAgent(agentGuid, new AgentActor.UpdateStatsCommand(message.RunningInstanceCount, message.RunningInstanceMemory));
	}
	
	private void HandleReportInstanceStatus(ReportInstanceStatusMessage message) {
		agentManager.TellAgent(agentGuid, new AgentActor.UpdateInstanceStatusCommand(message.InstanceGuid, message.InstanceStatus));
	}
	
	private void HandleReportInstancePlayerCounts(ReportInstancePlayerCountsMessage message) {
		agentManager.TellAgent(agentGuid, new AgentActor.UpdateInstancePlayerCountsCommand(message.InstanceGuid, message.PlayerCounts));
	}
	
	private void HandleReportInstanceEvent(ReportInstanceEventMessage message) {
		message.Event.Accept(eventLogManager.CreateInstanceEventVisitor(message.EventGuid, message.UtcTime, agentGuid, message.InstanceGuid));
	}
	
	private void HandleInstanceOutput(InstanceOutputMessage message) {
		instanceLogManager.ReceiveLines(message.InstanceGuid, message.Lines);
	}
}
