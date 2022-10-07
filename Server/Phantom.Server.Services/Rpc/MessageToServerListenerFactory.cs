using Phantom.Server.Rpc;
using Phantom.Server.Services.Agents;
using Phantom.Server.Services.Instances;

namespace Phantom.Server.Services.Rpc; 

public sealed class MessageToServerListenerFactory {
	private readonly ServiceConfiguration configuration;
	private readonly AgentManager agentManager;
	private readonly AgentJavaRuntimesManager agentJavaRuntimesManager;
	private readonly InstanceManager instanceManager;

	public MessageToServerListenerFactory(ServiceConfiguration configuration, AgentManager agentManager, AgentJavaRuntimesManager agentJavaRuntimesManager, InstanceManager instanceManager) {
		this.configuration = configuration;
		this.agentManager = agentManager;
		this.agentJavaRuntimesManager = agentJavaRuntimesManager;
		this.instanceManager = instanceManager;
	}

	public MessageToServerListener CreateListener(RpcClientConnection connection) {
		return new MessageToServerListener(connection, configuration, agentManager, agentJavaRuntimesManager, instanceManager);
	}
}
