using Akka.Actor;
using Phantom.Common.Messages.Web;
using Phantom.Utils.Actor;
using Phantom.Utils.Rpc.Runtime;
using Phantom.Utils.Tasks;
using Phantom.Web.Services.Agents;
using Phantom.Web.Services.Instances;

namespace Phantom.Web.Services.Rpc;

public sealed class ControllerMessageHandlerFactory {
	private readonly RpcConnectionToServer<IMessageToController> connection;
	private readonly AgentManager agentManager;
	private readonly InstanceManager instanceManager;
	private readonly InstanceLogManager instanceLogManager;
	
	private readonly TaskCompletionSource<bool> registerSuccessWaiter = AsyncTasks.CreateCompletionSource<bool>();
	
	public Task<bool> RegisterSuccessWaiter => registerSuccessWaiter.Task;
	
	private int messageHandlerId = 0;
	
	public ControllerMessageHandlerFactory(RpcConnectionToServer<IMessageToController> connection, AgentManager agentManager, InstanceManager instanceManager, InstanceLogManager instanceLogManager) {
		this.connection = connection;
		this.agentManager = agentManager;
		this.instanceManager = instanceManager;
		this.instanceLogManager = instanceLogManager;
	}
	
	public ActorRef<IMessageToWeb> Create(IActorRefFactory actorSystem) {
		int id = Interlocked.Increment(ref messageHandlerId);
		return actorSystem.ActorOf(ControllerMessageHandlerActor.Factory(new ControllerMessageHandlerActor.Init(connection, agentManager, instanceManager, instanceLogManager, registerSuccessWaiter)), "ControllerMessageHandler-" + id);
	}
}
