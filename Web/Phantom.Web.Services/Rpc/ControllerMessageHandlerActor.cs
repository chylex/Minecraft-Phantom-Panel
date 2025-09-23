using Phantom.Common.Messages.Web;
using Phantom.Common.Messages.Web.ToWeb;
using Phantom.Utils.Actor;
using Phantom.Web.Services.Agents;
using Phantom.Web.Services.Authentication;
using Phantom.Web.Services.Instances;

namespace Phantom.Web.Services.Rpc;

public sealed class ControllerMessageHandlerActor : ReceiveActor<IMessageToWeb> {
	public readonly record struct Init(
		AgentManager AgentManager,
		InstanceManager InstanceManager,
		InstanceLogManager InstanceLogManager,
		UserSessionRefreshManager UserSessionRefreshManager
	);
	
	public static Props<IMessageToWeb> Factory(Init init) {
		return Props<IMessageToWeb>.Create(() => new ControllerMessageHandlerActor(init), new ActorConfiguration { SupervisorStrategy = SupervisorStrategies.Resume });
	}
	
	private readonly AgentManager agentManager;
	private readonly InstanceManager instanceManager;
	private readonly InstanceLogManager instanceLogManager;
	private readonly UserSessionRefreshManager userSessionRefreshManager;
	
	private ControllerMessageHandlerActor(Init init) {
		this.agentManager = init.AgentManager;
		this.instanceManager = init.InstanceManager;
		this.instanceLogManager = init.InstanceLogManager;
		this.userSessionRefreshManager = init.UserSessionRefreshManager;
		
		Receive<RefreshAgentsMessage>(HandleRefreshAgents);
		Receive<RefreshInstancesMessage>(HandleRefreshInstances);
		Receive<InstanceOutputMessage>(HandleInstanceOutput);
		Receive<RefreshUserSessionMessage>(HandleRefreshUserSession);
	}
	
	private void HandleRefreshAgents(RefreshAgentsMessage message) {
		agentManager.RefreshAgents(message.Agents);
	}
	
	private void HandleRefreshInstances(RefreshInstancesMessage message) {
		instanceManager.RefreshInstances(message.Instances);
	}
	
	private void HandleInstanceOutput(InstanceOutputMessage message) {
		instanceLogManager.AddLines(message.InstanceGuid, message.Lines);
	}
	
	private void HandleRefreshUserSession(RefreshUserSessionMessage message) {
		userSessionRefreshManager.RefreshUser(message.UserGuid);
	}
}
