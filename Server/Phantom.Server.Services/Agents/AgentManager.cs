using System.Collections.Immutable;
using Phantom.Common.Data;
using Phantom.Common.Messages;
using Phantom.Common.Messages.ToServer;
using Phantom.Server.Rpc;
using Phantom.Server.Services.Instances;
using Phantom.Utils.Collections;
using Phantom.Utils.Events;
using Phantom.Utils.Logging;
using Serilog;

namespace Phantom.Server.Services.Agents;

public sealed class AgentManager {
	private static readonly ILogger Logger = PhantomLogger.Create<AgentManager>();

	private readonly ObservableAgentInfos agentInfos = new ();
	private readonly ObservableAgentStats agentStats = new ();

	public AgentAuthToken AuthToken { get; }
	public EventSubscribers<ImmutableArray<AgentInfo>> AgentsInfosChanged => agentInfos.Subs;
	public EventSubscribers<ImmutableArray<AgentStats>> AgentStatsChanged => agentStats.Subs;

	internal AgentManager(AgentAuthToken authToken, InstanceManager instanceManager) {
		this.AuthToken = authToken;

		AgentsInfosChanged.Subscribe(this, agentStats.UpdateAgents);
		instanceManager.InstancesChanged.Subscribe(this, agentStats.UpdateInstances);
	}

	internal RegisterAgentResult RegisterAgent(RegisterAgentMessage message, RpcClientConnection connection) {
		if (!AuthToken.FixedTimeEquals(message.AuthToken)) {
			return RegisterAgentResult.InvalidToken;
		}
		else if (!agentInfos.TryRegister(new AgentConnection(connection, message.AgentInfo))) {
			return RegisterAgentResult.OldConnectionNotClosed;
		}
		else {
			Logger.Information("Registered agent \"{Name}\" (GUID {Guid}).", message.AgentInfo.Name, message.AgentInfo.Guid);
			return RegisterAgentResult.Success;
		}
	}

	internal void UnregisterAgent(UnregisterAgentMessage message, RpcClientConnection connection) {
		if (agentInfos.TryUnregister(message.AgentGuid, connection)) {
			Logger.Information("Unregistered agent with GUID {Guid}.", message.AgentGuid);
		}
	}

	public ImmutableDictionary<Guid, AgentStats> GetAgentStats() {
		return agentStats.GetAgentStats();
	}

	public AgentStats? GetAgentStats(Guid agentGuid) {
		return agentStats.GetAgentStats(agentGuid);
	}

	public async Task SendMessage<TMessage>(Guid guid, TMessage message) where TMessage : IMessageToAgent {
		var connection = agentInfos.GetConnection(guid);
		if (connection != null) {
			await connection.SendMessage(message);
		}
		// TODO handle missing agent?
	}

	private sealed class ObservableAgentInfos : ObservableState<ImmutableArray<AgentInfo>> {
		private readonly RwLockedDictionary<Guid, AgentConnection> agents = new (LockRecursionPolicy.NoRecursion);

		public bool TryRegister(AgentConnection agentConnection) {
			if (agents.TryAddOrReplace(agentConnection.Info.Guid, agentConnection, static oldConnection => oldConnection.IsClosed)) {
				Update();
				return true;
			}
			else {
				return false;
			}
		}

		public bool TryUnregister(Guid guid, RpcClientConnection connection) {
			if (agents.TryRemove(guid, oldConnection => oldConnection.IsSame(connection))) {
				Update();
				return true;
			}
			else {
				return false;
			}
		}

		public AgentConnection? GetConnection(Guid guid) {
			return agents.TryGetValue(guid, out var connection) ? connection : null;
		}

		protected override ImmutableArray<AgentInfo> GetData() {
			return agents.ValuesCopy.Select(static agent => agent.Info).ToImmutableArray();
		}
	}

	private sealed class ObservableAgentStats : ObservableState<ImmutableArray<AgentStats>> {
		private ImmutableDictionary<Guid, AgentInfo> agents = ImmutableDictionary<Guid, AgentInfo>.Empty;
		private ImmutableDictionary<Guid, ImmutableArray<InstanceInfo>> instancesByAgentGuid = ImmutableDictionary<Guid, ImmutableArray<InstanceInfo>>.Empty;

		public void UpdateAgents(ImmutableArray<AgentInfo> newAgents) {
			agents = newAgents.ToImmutableDictionary(static agent => agent.Guid);
			Update();
		}

		public void UpdateInstances(ImmutableArray<InstanceInfo> newInstances) {
			instancesByAgentGuid = newInstances.GroupBy(static instance => instance.AgentGuid, static (agentGuid, instances) => KeyValuePair.Create(agentGuid, instances.ToImmutableArray())).ToImmutableDictionary();
			Update();
		}

		public AgentStats? GetAgentStats(Guid agentGuid) {
			return agents.TryGetValue(agentGuid, out var agentInfo) ? ComputeAgentStats(instancesByAgentGuid, agentInfo) : null;
		}

		public ImmutableDictionary<Guid, AgentStats> GetAgentStats() {
			return agents.Values.Select(agent => ComputeAgentStats(instancesByAgentGuid, agent)).ToImmutableDictionary(static stats => stats.AgentInfo.Guid);
		}

		protected override ImmutableArray<AgentStats> GetData() {
			return agents.Values.Select(agent => ComputeAgentStats(instancesByAgentGuid, agent)).ToImmutableArray();
		}

		private static AgentStats ComputeAgentStats(ImmutableDictionary<Guid, ImmutableArray<InstanceInfo>> instancesByAgentGuid, AgentInfo agentInfo) {
			int usedInstances = 0;
			var usedMemory = RamAllocationUnits.Zero;

			if (instancesByAgentGuid.TryGetValue(agentInfo.Guid, out var instances)) {
				foreach (var instance in instances) {
					usedInstances += 1;
					usedMemory += instance.MemoryAllocation;
				}
			}

			return new AgentStats(agentInfo, usedInstances, usedMemory);
		}
	}
}
