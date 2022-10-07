using Phantom.Server.Rpc;
using Phantom.Server.Services.Agents;

namespace Phantom.Server.Services.Rpc; 

public sealed class MessageToServerListenerFactory {
	private readonly ServiceConfiguration configuration;
	private readonly AgentManager agentManager;
	private readonly AgentJavaRuntimesManager agentJavaRuntimesManager;

	public MessageToServerListenerFactory(ServiceConfiguration configuration, AgentManager agentManager, AgentJavaRuntimesManager agentJavaRuntimesManager) {
		this.configuration = configuration;
		this.agentManager = agentManager;
		this.agentJavaRuntimesManager = agentJavaRuntimesManager;
	}

	public MessageToServerListener CreateListener(RpcClientConnection connection) {
		return new MessageToServerListener(connection, configuration, agentManager, agentJavaRuntimesManager);
	}
}
