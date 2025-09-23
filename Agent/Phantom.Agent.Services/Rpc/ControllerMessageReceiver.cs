using Phantom.Common.Messages.Agent;
using Phantom.Utils.Actor;
using Phantom.Utils.Rpc.Message;

namespace Phantom.Agent.Services.Rpc;

public sealed class ControllerMessageReceiver(ActorRef<IMessageToAgent> actor, AgentRegistrationHandler agentRegistrationHandler) : IMessageReceiver<IMessageToAgent>.Actor(actor) {
	public override void OnSessionRestarted() {
		agentRegistrationHandler.OnNewSession();
	}
}
