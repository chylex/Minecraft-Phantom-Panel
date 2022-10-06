using Phantom.Server.Rpc;
using Phantom.Server.Services.Agents;

namespace Phantom.Server.Services.Rpc; 

public sealed class MessageToServerListenerFactory {
	private readonly ServiceConfiguration configuration;
	private readonly AgentManager agentManager;

	public MessageToServerListenerFactory(ServiceConfiguration configuration, AgentManager agentManager) {
		this.configuration = configuration;
		this.agentManager = agentManager;
	}

	public MessageToServerListener CreateListener(RpcClientConnection connection) {
		return new MessageToServerListener(connection, configuration, agentManager);
	}
}
