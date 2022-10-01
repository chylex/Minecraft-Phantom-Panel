using Phantom.Server.Rpc;
using Phantom.Server.Services.Agents;
using Phantom.Server.Services.Instances;

namespace Phantom.Server.Services.Rpc; 

public sealed class MessageToServerListenerFactory {
	private readonly ServiceConfiguration configuration;
	private readonly AgentManager agentManager;
	private readonly AgentJavaRuntimesManager agentJavaRuntimesManager;
	private readonly InstanceManager instanceManager;
	private readonly InstanceLogManager instanceLogManager;

	public MessageToServerListenerFactory(ServiceConfiguration configuration, AgentManager agentManager, AgentJavaRuntimesManager agentJavaRuntimesManager, InstanceManager instanceManager, InstanceLogManager instanceLogManager) {
		this.configuration = configuration;
		this.agentManager = agentManager;
		this.agentJavaRuntimesManager = agentJavaRuntimesManager;
		this.instanceManager = instanceManager;
		this.instanceLogManager = instanceLogManager;
	}

	public MessageToServerListener CreateListener(RpcClientConnection connection) {
		return new MessageToServerListener(connection, configuration, agentManager, agentJavaRuntimesManager, instanceManager, instanceLogManager);
	}
}
