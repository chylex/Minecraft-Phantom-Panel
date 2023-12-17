using System.Collections.Immutable;
using Phantom.Common.Data.Web.Agent;
using Phantom.Utils.Events;
using Phantom.Utils.Logging;

namespace Phantom.Web.Services.Agents; 

public sealed class AgentManager {
	private readonly SimpleObservableState<ImmutableArray<AgentWithStats>> agents = new (PhantomLogger.Create<AgentManager>("Agents"), ImmutableArray<AgentWithStats>.Empty);

	public EventSubscribers<ImmutableArray<AgentWithStats>> AgentsChanged => agents.Subs;

	internal void RefreshAgents(ImmutableArray<AgentWithStats> newAgents) {
		agents.SetTo(newAgents);
	}

	public ImmutableArray<AgentWithStats> GetAll() {
		return agents.Value;
	}
	
	public ImmutableDictionary<Guid, AgentWithStats> ToDictionaryByGuid() {
		return agents.Value.ToImmutableDictionary(static agent => agent.Guid);
	}
}
