using Phantom.Common.Messages.Web;
using Phantom.Common.Messages.Web.BiDirectional;
using Phantom.Common.Messages.Web.ToWeb;
using Phantom.Utils.Actor;
using Phantom.Utils.Rpc.Runtime;
using Phantom.Web.Services.Agents;
using Phantom.Web.Services.Authentication;
using Phantom.Web.Services.Instances;

namespace Phantom.Web.Services.Rpc;

sealed class ControllerMessageHandlerActor : ReceiveActor<IMessageToWeb> {
	public readonly record struct Init(
		RpcConnectionToServer<IMessageToController> Connection,
		AgentManager AgentManager,
		InstanceManager InstanceManager,
		InstanceLogManager InstanceLogManager,
		UserSessionRefreshManager UserSessionRefreshManager,
		TaskCompletionSource<bool> RegisterSuccessWaiter
	);
	
	public static Props<IMessageToWeb> Factory(Init init) {
		return Props<IMessageToWeb>.Create(() => new ControllerMessageHandlerActor(init), new ActorConfiguration { SupervisorStrategy = SupervisorStrategies.Resume });
	}
	
	private readonly RpcConnectionToServer<IMessageToController> connection;
	private readonly AgentManager agentManager;
	private readonly InstanceManager instanceManager;
	private readonly InstanceLogManager instanceLogManager;
	private readonly UserSessionRefreshManager userSessionRefreshManager;
	private readonly TaskCompletionSource<bool> registerSuccessWaiter;
	
	private ControllerMessageHandlerActor(Init init) {
		this.connection = init.Connection;
		this.agentManager = init.AgentManager;
		this.instanceManager = init.InstanceManager;
		this.instanceLogManager = init.InstanceLogManager;
		this.userSessionRefreshManager = init.UserSessionRefreshManager;
		this.registerSuccessWaiter = init.RegisterSuccessWaiter;
		
		Receive<RegisterWebResultMessage>(HandleRegisterWebResult);
		Receive<RefreshAgentsMessage>(HandleRefreshAgents);
		Receive<RefreshInstancesMessage>(HandleRefreshInstances);
		Receive<InstanceOutputMessage>(HandleInstanceOutput);
		Receive<RefreshUserSessionMessage>(HandleRefreshUserSession);
		Receive<ReplyMessage>(HandleReply);
	}
	
	private void HandleRegisterWebResult(RegisterWebResultMessage message) {
		registerSuccessWaiter.TrySetResult(message.Success);
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
	
	private void HandleReply(ReplyMessage message) {
		connection.Receive(message);
	}
}
