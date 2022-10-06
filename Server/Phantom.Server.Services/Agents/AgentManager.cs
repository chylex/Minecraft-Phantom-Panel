using System.Collections.Immutable;
using Phantom.Common.Data.Agent;
using Phantom.Common.Data.Replies;
using Phantom.Common.Logging;
using Phantom.Common.Messages.ToAgent;
using Phantom.Server.Rpc;
using Phantom.Utils.Collections;
using Phantom.Utils.Events;
using Serilog;

namespace Phantom.Server.Services.Agents;

public sealed class AgentManager {
	private static readonly ILogger Logger = PhantomLogger.Create<AgentManager>();

	private readonly ObservableAgents agents = new (PhantomLogger.Create<AgentManager, ObservableAgents>());

	public EventSubscribers<ImmutableArray<Agent>> AgentsChanged => agents.Subs;

	private readonly CancellationToken cancellationToken;
	private readonly AgentAuthToken authToken;

	public AgentManager(ServiceConfiguration configuration, AgentAuthToken authToken) {
		this.cancellationToken = configuration.CancellationToken;
		this.authToken = authToken;
	}

	internal async Task<bool> RegisterAgent(AgentAuthToken authToken, AgentInfo agentInfo, RpcClientConnection connection) {
		if (!this.authToken.FixedTimeEquals(authToken)) {
			await connection.Send(new RegisterAgentFailureMessage(RegisterAgentFailure.InvalidToken));
			return false;
		}

		var agent = new Agent(agentInfo) {
			LastPing = DateTimeOffset.Now,
			Connection = new AgentConnection(connection)
		};

		agents.Register(agent);

		Logger.Information("Registered agent \"{Name}\" (GUID {Guid}).", agent.Name, agent.Guid);

		await connection.Send(new RegisterAgentSuccessMessage());
		return true;
	}

	internal void UnregisterAgent(Guid agentGuid, RpcClientConnection connection) {
		if (agents.TryUnregister(agentGuid, connection)) {
			Logger.Information("Unregistered agent with GUID {Guid}.", agentGuid);
		}
	}

	internal Agent? GetAgent(Guid guid) {
		return agents.GetAgent(guid);
	}

	internal void NotifyAgentIsAlive(Guid agentGuid) {
		// TODO automatically mark agent as offline if it doesn't send pings
		agents.Update(agentGuid, static agent => agent with { LastPing = DateTimeOffset.Now });
	}

	private sealed class ObservableAgents : ObservableState<ImmutableArray<Agent>> {
		private readonly RwLockedDictionary<Guid, Agent> agents = new (LockRecursionPolicy.NoRecursion);

		public ObservableAgents(ILogger logger) : base(logger) {}

		public void Register(Agent agent) {
			if (agents.AddOrReplace(agent.Guid, agent, out var oldAgent) && oldAgent.Connection is {} oldConnection) {
				oldConnection.Close();
			}

			Update();
		}

		public void Update(Guid guid, Func<Agent, Agent> updater) {
			UpdateIf(agents.TryReplace(guid, updater));
		}

		public bool TryUnregister(Guid guid, RpcClientConnection connection) {
			return UpdateIf(agents.TryReplaceIf(guid, static oldAgent => oldAgent.AsOffline(), oldAgent => oldAgent.Connection?.IsSame(connection) == true));
		}

		public Agent? GetAgent(Guid guid) {
			return agents.TryGetValue(guid, out var agent) ? agent : null;
		}

		protected override ImmutableArray<Agent> GetData() {
			return agents.ValuesCopy;
		}
	}
}
