using Akka.Actor;
using Phantom.Common.Messages.Web;
using Phantom.Utils.Actor;
using Phantom.Utils.Rpc.Runtime;
using Phantom.Utils.Tasks;
using Phantom.Web.Services.Agents;
using Phantom.Web.Services.Authentication;
using Phantom.Web.Services.Instances;

namespace Phantom.Web.Services.Rpc;

public sealed class ControllerMessageHandlerFactory {
	private readonly RpcConnectionToServer<IMessageToController> connection;
	private readonly AgentManager agentManager;
	private readonly InstanceManager instanceManager;
	private readonly InstanceLogManager instanceLogManager;
	private readonly UserSessionRefreshManager userSessionRefreshManager;
	
	private readonly TaskCompletionSource<bool> registerSuccessWaiter = AsyncTasks.CreateCompletionSource<bool>();
	
	public Task<bool> RegisterSuccessWaiter => registerSuccessWaiter.Task;
	
	private int messageHandlerId = 0;
	
	public ControllerMessageHandlerFactory(RpcConnectionToServer<IMessageToController> connection, AgentManager agentManager, InstanceManager instanceManager, InstanceLogManager instanceLogManager, UserSessionRefreshManager userSessionRefreshManager) {
		this.connection = connection;
		this.agentManager = agentManager;
		this.instanceManager = instanceManager;
		this.instanceLogManager = instanceLogManager;
		this.userSessionRefreshManager = userSessionRefreshManager;
	}
	
	public ActorRef<IMessageToWeb> Create(IActorRefFactory actorSystem) {
		var init = new ControllerMessageHandlerActor.Init(connection, agentManager, instanceManager, instanceLogManager, userSessionRefreshManager, registerSuccessWaiter);
		var name = "ControllerMessageHandler-" + Interlocked.Increment(ref messageHandlerId);
		return actorSystem.ActorOf(ControllerMessageHandlerActor.Factory(init), name);
	}
}
