using System.Collections.Concurrent;
using System.Collections.Immutable;
using Phantom.Common.Rpc.Messages.ToAgent;
using Phantom.Common.Rpc.Messages.ToServer;
using Phantom.Server.Rpc;
using Phantom.Utils.Events;

namespace Phantom.Server.Services.Agents;

public sealed class AgentManager {
	private readonly ObservableAgents agents = new ();

	public AgentAuthToken AuthToken { get; }
	public EventSubscribers<ImmutableList<AgentInfo>> AgentInfoChanged => agents.Subs;

	internal AgentManager(AgentAuthToken authToken) {
		this.AuthToken = authToken;
	}

	internal RegisterAgentResultMessage RegisterAgent(RegisterAgentMessage message, RpcClientConnection connection) {
		if (!AuthToken.Check(message.AuthToken)) {
			return RegisterAgentResultMessage.WithError("Invalid auth token.");
		}
		else if (!agents.TryRegister(new AgentInfo(message.AgentGuid, connection, message.AgentVersion, message.AgentName))) {
			return RegisterAgentResultMessage.WithError("Agent registration failed.");
		}
		else {
			return RegisterAgentResultMessage.WithSuccess;
		}
	}

	private sealed class ObservableAgents : ObservableState<ImmutableList<AgentInfo>> {
		private readonly ConcurrentDictionary<Guid, AgentInfo> agents = new ();

		public bool TryRegister(AgentInfo agentInfo) {
			if (agents.TryAdd(agentInfo.Guid, agentInfo)) {
				Update();
				return true;
			}
			else {
				return false;
			}
		}

		protected override ImmutableList<AgentInfo> GetData() {
			return ImmutableList.CreateRange(agents.Values);
		}
	}
}
