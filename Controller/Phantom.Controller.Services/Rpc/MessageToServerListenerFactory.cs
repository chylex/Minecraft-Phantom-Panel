using Phantom.Controller.Rpc;
using Phantom.Controller.Services.Agents;
using Phantom.Controller.Services.Events;
using Phantom.Controller.Services.Instances;

namespace Phantom.Controller.Services.Rpc; 

public sealed class MessageToServerListenerFactory {
	private readonly ServiceConfiguration configuration;
	private readonly AgentManager agentManager;
	private readonly AgentJavaRuntimesManager agentJavaRuntimesManager;
	private readonly InstanceManager instanceManager;
	private readonly InstanceLogManager instanceLogManager;
	private readonly EventLog eventLog;

	public MessageToServerListenerFactory(ServiceConfiguration configuration, AgentManager agentManager, AgentJavaRuntimesManager agentJavaRuntimesManager, InstanceManager instanceManager, InstanceLogManager instanceLogManager, EventLog eventLog) {
		this.configuration = configuration;
		this.agentManager = agentManager;
		this.agentJavaRuntimesManager = agentJavaRuntimesManager;
		this.instanceManager = instanceManager;
		this.instanceLogManager = instanceLogManager;
		this.eventLog = eventLog;
	}

	public MessageToServerListener CreateListener(RpcClientConnection connection) {
		return new MessageToServerListener(connection, configuration, agentManager, agentJavaRuntimesManager, instanceManager, instanceLogManager, eventLog);
	}
}
