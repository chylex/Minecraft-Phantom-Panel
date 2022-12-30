using System.Collections.Immutable;
using Phantom.Common.Data;
using Phantom.Common.Logging;
using Phantom.Server.Services.Instances;
using Phantom.Utils.Events;
using ILogger = Serilog.ILogger;

namespace Phantom.Server.Services.Agents; 

public sealed class AgentStatsManager {
	private readonly ObservableAgentStats agentStats = new (PhantomLogger.Create<AgentManager, ObservableAgentStats>());
	
	public EventSubscribers<ImmutableArray<AgentStats>> AgentStatsChanged => agentStats.Subs;
	
	public AgentStatsManager(AgentManager agentManager, InstanceManager instanceManager) {
		agentManager.AgentsChanged.Subscribe(this, agentStats.UpdateAgents);
		instanceManager.InstancesChanged.Subscribe(this, agentStats.UpdateInstances);
	}
	
	public ImmutableDictionary<Guid, AgentStats> GetOnlineAgentStats() {
		return agentStats.GetOnlineAgentStats();
	}

	public AgentStats? GetAgentStats(Guid agentGuid) {
		return agentStats.GetAgentStats(agentGuid);
	}

	private sealed class ObservableAgentStats : ObservableState<ImmutableArray<AgentStats>> {
		private ImmutableDictionary<Guid, Agent> agents = ImmutableDictionary<Guid, Agent>.Empty;
		private ImmutableDictionary<Guid, ImmutableArray<Instance>> instancesByAgentGuid = ImmutableDictionary<Guid, ImmutableArray<Instance>>.Empty;

		public ObservableAgentStats(ILogger logger) : base(logger) {}

		public void UpdateAgents(ImmutableArray<Agent> newAgents) {
			agents = newAgents.ToImmutableDictionary(static agent => agent.Guid, static agent => agent);
			Update();
		}

		public void UpdateInstances(ImmutableDictionary<Guid, Instance> newInstances) {
			instancesByAgentGuid = newInstances.Values.GroupBy(static instance => instance.Configuration.AgentGuid, static (agentGuid, instances) => KeyValuePair.Create(agentGuid, instances.ToImmutableArray())).ToImmutableDictionary();
			Update();
		}

		public AgentStats? GetAgentStats(Guid agentGuid) {
			return agents.TryGetValue(agentGuid, out var agent) ? ComputeAgentStats(instancesByAgentGuid, agent) : null;
		}

		public ImmutableDictionary<Guid, AgentStats> GetOnlineAgentStats() {
			return agents.Values
			             .Where(static agent => agent.IsOnline)
			             .Select(agent => ComputeAgentStats(instancesByAgentGuid, agent))
			             .ToImmutableDictionary(static stats => stats.Agent.Guid);
		}

		protected override ImmutableArray<AgentStats> GetData() {
			return agents.Values
			             .Select(agent => ComputeAgentStats(instancesByAgentGuid, agent))
			             .ToImmutableArray();
		}

		private static AgentStats ComputeAgentStats(ImmutableDictionary<Guid, ImmutableArray<Instance>> instancesByAgentGuid, Agent agent) {
			int usedInstances = 0;
			var usedMemory = RamAllocationUnits.Zero;

			if (instancesByAgentGuid.TryGetValue(agent.Guid, out var instances)) {
				foreach (var instance in instances) {
					usedInstances += 1;
					usedMemory += instance.Configuration.MemoryAllocation;
				}
			}

			return new AgentStats(agent, usedInstances, usedMemory);
		}
	}
}
