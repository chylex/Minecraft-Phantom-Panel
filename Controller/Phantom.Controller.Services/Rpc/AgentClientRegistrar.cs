using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Akka.Actor;
using Phantom.Common.Data.Agent;
using Phantom.Common.Messages.Agent;
using Phantom.Controller.Services.Agents;
using Phantom.Controller.Services.Events;
using Phantom.Controller.Services.Instances;
using Phantom.Utils.Actor;
using Phantom.Utils.Rpc.Message;
using Phantom.Utils.Rpc.Runtime.Server;

namespace Phantom.Controller.Services.Rpc;

sealed class AgentClientRegistrar(
	IActorRefFactory actorSystem,
	AgentManager agentManager,
	InstanceLogManager instanceLogManager,
	EventLogManager eventLogManager
) : IRpcServerClientRegistrar<IMessageToController, IMessageToAgent, AgentInfo> {
	private readonly ConcurrentDictionary<Guid, Receiver> receiversBySessionGuid = new ();
	
	[SuppressMessage("ReSharper", "LambdaShouldNotCaptureContext")]
	public IMessageReceiver<IMessageToController> Register(RpcServerToClientConnection<IMessageToController, IMessageToAgent> connection, AgentInfo handshakeResult) {
		var agentGuid = handshakeResult.AgentGuid;
		agentManager.TellAgent(agentGuid, new AgentActor.SetConnectionCommand(connection));
		
		var receiver = receiversBySessionGuid.GetOrAdd(connection.SessionId, CreateReceiver, agentGuid);
		if (receiver.AgentGuid != agentGuid) {
			throw new InvalidOperationException("Cannot register two agents to the same session!");
		}
		
		return receiver;
	}
	
	private Receiver CreateReceiver(Guid sessionId, Guid agentGuid) {
		var name = "AgentClient-" + sessionId;
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
