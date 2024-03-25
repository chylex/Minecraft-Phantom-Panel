using System.Collections.Immutable;
using Phantom.Agent.Services.Instances;
using Phantom.Utils.Actor.Event;

namespace Phantom.Agent.Services;

sealed class AgentState {
	private readonly ObservableState<ImmutableDictionary<Guid, Instance>> instancesByGuid = new (ImmutableDictionary<Guid, Instance>.Empty);
	
	public ImmutableDictionary<Guid, Instance> InstancesByGuid => instancesByGuid.State;
	
	public void UpdateInstance(Instance instance) {
		instancesByGuid.PublisherSide.Publish(static (instancesByGuid, instance) => instancesByGuid.SetItem(instance.InstanceGuid, instance), instance);
	}
}
