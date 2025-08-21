using Phantom.Common.Messages.Agent;
using Phantom.Common.Messages.Agent.ToController;
using Phantom.Controller.Services.Agents;
using Phantom.Controller.Services.Events;
using Phantom.Controller.Services.Instances;
using Phantom.Utils.Actor;
using Phantom.Utils.Rpc.Runtime;

namespace Phantom.Controller.Services.Rpc;

sealed class AgentRegistrationHandler : IRegistrationHandler<IMessageToAgent, IMessageToController, RegisterAgentMessage> {
	private readonly AgentManager agentManager;
	private readonly InstanceLogManager instanceLogManager;
	private readonly EventLogManager eventLogManager;
	
	public AgentRegistrationHandler(AgentManager agentManager, InstanceLogManager instanceLogManager, EventLogManager eventLogManager) {
		this.agentManager = agentManager;
		this.instanceLogManager = instanceLogManager;
		this.eventLogManager = eventLogManager;
	}
	
	async Task<Props<IMessageToController>?> IRegistrationHandler<IMessageToAgent, IMessageToController, RegisterAgentMessage>.TryRegister(RpcConnectionToClient<IMessageToAgent> connection, RegisterAgentMessage message) {
		return await TryRegisterImpl(connection, message) ? CreateMessageHandlerActorProps(message.AgentInfo.AgentGuid, connection) : null;
	}
	
	public Task<bool> TryRegisterImpl(RpcConnectionToClient<IMessageToAgent> connection, RegisterAgentMessage message) {
		return agentManager.RegisterAgent(message.AuthToken, message.AgentInfo, connection);
	}
	
	private Props<IMessageToController> CreateMessageHandlerActorProps(Guid agentGuid, RpcConnectionToClient<IMessageToAgent> connection) {
		var init = new AgentMessageHandlerActor.Init(agentGuid, connection, this, agentManager, instanceLogManager, eventLogManager);
		return AgentMessageHandlerActor.Factory(init);
	}
}
