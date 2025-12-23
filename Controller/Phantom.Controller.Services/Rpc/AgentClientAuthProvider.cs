using Phantom.Controller.Services.Agents;
using Phantom.Utils.Rpc;
using Phantom.Utils.Rpc.Runtime.Server;

namespace Phantom.Controller.Services.Rpc;

sealed class AgentClientAuthProvider(AgentManager agentManager) : IRpcServerClientAuthProvider {
	public Task<AuthSecret?> GetAuthSecret(Guid agentGuid) {
		return agentManager.GetAgentAuthSecret(agentGuid);
	}
}
