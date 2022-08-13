using System.Collections.Concurrent;
using Phantom.Common.Rpc.Messages.ToAgent;
using Phantom.Common.Rpc.Messages.ToServer;
using Phantom.Server.Rpc;

namespace Phantom.Server.Services.Agents;

public sealed class AgentManager {
	private readonly ConcurrentDictionary<Guid, AgentInfo> agents = new ();

	public AgentAuthToken AuthToken { get; }
	
	internal AgentManager(AgentAuthToken authToken) {
		this.AuthToken = authToken;
	}

	internal RegisterAgentResultMessage RegisterAgent(RegisterAgentMessage message, RpcClientConnection connection) {
		if (!AuthToken.Check(message.AuthToken)) {
			return RegisterAgentResultMessage.WithError("Invalid auth token.");
		}
		else if (!agents.TryAdd(message.AgentGuid, new AgentInfo(connection, message.AgentVersion))) {
			return RegisterAgentResultMessage.WithError("Agent registration failed.");
		}
		else {
			return RegisterAgentResultMessage.WithSuccess;
		}
	}
}
