using System.Collections.Immutable;
using Phantom.Common.Data.Java;
using Phantom.Common.Data.Web.Agent;
using Phantom.Common.Data.Web.Instance;
using Phantom.Utils.Actor.Event;

namespace Phantom.Controller.Services;

sealed class ControllerState {
	private readonly ObservableState<ImmutableDictionary<Guid, Agent>> agentsByGuid = new (ImmutableDictionary<Guid, Agent>.Empty);
	private readonly ObservableState<ImmutableDictionary<Guid, ImmutableArray<TaggedJavaRuntime>>> agentJavaRuntimesByGuid = new (ImmutableDictionary<Guid, ImmutableArray<TaggedJavaRuntime>>.Empty);
	private readonly ObservableState<ImmutableDictionary<Guid, Instance>> instancesByGuid = new (ImmutableDictionary<Guid, Instance>.Empty);
	
	public ImmutableDictionary<Guid, Agent> AgentsByGuid => agentsByGuid.State;
	public ImmutableDictionary<Guid, ImmutableArray<TaggedJavaRuntime>> AgentJavaRuntimesByGuid => agentJavaRuntimesByGuid.State;
	public ImmutableDictionary<Guid, Instance> InstancesByGuid => instancesByGuid.State;
	
	public ObservableState<ImmutableDictionary<Guid, Agent>>.Receiver AgentsByGuidReceiver => agentsByGuid.ReceiverSide;
	public ObservableState<ImmutableDictionary<Guid, ImmutableArray<TaggedJavaRuntime>>>.Receiver AgentJavaRuntimesByGuidReceiver => agentJavaRuntimesByGuid.ReceiverSide;
	public ObservableState<ImmutableDictionary<Guid, Instance>>.Receiver InstancesByGuidReceiver => instancesByGuid.ReceiverSide;
	
	public event EventHandler<Guid>? UserUpdatedOrDeleted;
	
	public void UpdateAgent(Agent agent) {
		agentsByGuid.PublisherSide.Publish(static (agentsByGuid, agent) => agentsByGuid.SetItem(agent.AgentGuid, agent), agent);
	}
	
	public void UpdateAgentJavaRuntimes(Guid agentGuid, ImmutableArray<TaggedJavaRuntime> runtimes) {
		agentJavaRuntimesByGuid.PublisherSide.Publish(static (agentJavaRuntimesByGuid, agentGuid, runtimes) => agentJavaRuntimesByGuid.SetItem(agentGuid, runtimes), agentGuid, runtimes);
	}
	
	public void UpdateInstance(Instance instance) {
		instancesByGuid.PublisherSide.Publish(static (instancesByGuid, instance) => instancesByGuid.SetItem(instance.InstanceGuid, instance), instance);
	}
	
	public void UpdateOrDeleteUser(Guid userGuid) {
		UserUpdatedOrDeleted?.Invoke(null, userGuid);
	}
}
