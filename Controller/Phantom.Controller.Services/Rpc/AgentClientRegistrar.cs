using System.Collections.Concurrent;
using Akka.Actor;
using Phantom.Common.Messages.Agent;
using Phantom.Controller.Services.Agents;
using Phantom.Controller.Services.Events;
using Phantom.Controller.Services.Instances;
using Phantom.Utils.Actor;
using Phantom.Utils.Rpc.Message;
using Phantom.Utils.Rpc.Runtime.Server;

namespace Phantom.Controller.Services.Rpc;

sealed class AgentClientRegistrar : IRpcServerClientRegistrar<IMessageToController, IMessageToAgent> {
	private readonly IActorRefFactory actorSystem;
	private readonly AgentManager agentManager;
	private readonly InstanceLogManager instanceLogManager;
	private readonly EventLogManager eventLogManager;
	
	private readonly Func<Guid, Guid, Receiver> receiverFactory;
	private readonly ConcurrentDictionary<Guid, Receiver> receiversBySessionGuid = new ();
	
	public AgentClientRegistrar(IActorRefFactory actorSystem, AgentManager agentManager, InstanceLogManager instanceLogManager, EventLogManager eventLogManager) {
		this.actorSystem = actorSystem;
		this.agentManager = agentManager;
		this.instanceLogManager = instanceLogManager;
		this.eventLogManager = eventLogManager;
		this.receiverFactory = CreateReceiver;
	}
	
	public IMessageReceiver<IMessageToController> Register(RpcServerToClientConnection<IMessageToController, IMessageToAgent> connection) {
		Guid agentGuid = connection.ClientGuid;
		
		agentManager.TellAgent(agentGuid, new AgentActor.SetConnectionCommand(connection));
		
		var receiver = receiversBySessionGuid.GetOrAdd(connection.SessionGuid, receiverFactory, agentGuid);
		if (receiver.AgentGuid != agentGuid) {
			throw new InvalidOperationException("Cannot register two agents to the same session!");
		}
		
		return receiver;
	}
	
	private Receiver CreateReceiver(Guid sessionGuid, Guid agentGuid) {
		var name = "AgentClient-" + sessionGuid;
		var init = new AgentMessageHandlerActor.Init(agentGuid, agentManager, instanceLogManager, eventLogManager);
		return new Receiver(agentGuid, agentManager, actorSystem.ActorOf(AgentMessageHandlerActor.Factory(init), name));
	}
	
	private sealed class Receiver(Guid agentGuid, AgentManager agentManager, ActorRef<IMessageToController> actor) : IMessageReceiver<IMessageToController>.Actor(actor) {
		public Guid AgentGuid => agentGuid;
		
		public override Task OnSessionTerminated() {
			agentManager.TellAgent(agentGuid, new AgentActor.UnregisterCommand());
			return base.OnSessionTerminated();
		}
		
		public override void OnPing() {
			agentManager.TellAgent(agentGuid, new AgentActor.NotifyIsAliveCommand());
		}
	}
}
