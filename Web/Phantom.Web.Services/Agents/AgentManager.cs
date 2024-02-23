using System.Collections.Immutable;
using Phantom.Common.Data.Web.Agent;
using Phantom.Utils.Events;
using Phantom.Utils.Logging;

namespace Phantom.Web.Services.Agents; 

public sealed class AgentManager {
	private readonly SimpleObservableState<ImmutableArray<Agent>> agents = new (PhantomLogger.Create<AgentManager>("Agents"), ImmutableArray<Agent>.Empty);

	public EventSubscribers<ImmutableArray<Agent>> AgentsChanged => agents.Subs;

	internal void RefreshAgents(ImmutableArray<Agent> newAgents) {
		agents.SetTo(newAgents);
	}

	public ImmutableArray<Agent> GetAll() {
		return agents.Value;
	}
	
	public ImmutableDictionary<Guid, Agent> ToDictionaryByGuid() {
		return agents.Value.ToImmutableDictionary(static agent => agent.AgentGuid);
	}
}
