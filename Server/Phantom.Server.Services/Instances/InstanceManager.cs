using System.Collections.Immutable;
using Phantom.Utils.Collections;
using Phantom.Utils.Events;
using Phantom.Utils.Logging;
using Serilog;

namespace Phantom.Server.Services.Instances; 

public sealed class InstanceManager {
	private static readonly ILogger Logger = PhantomLogger.Create<InstanceManager>();
	
	private readonly ObservableInstances instances = new ();
	
	public EventSubscribers<ImmutableArray<InstanceInfo>> InstancesChanged => instances.Subs;

	internal InstanceManager() {}

	public AddInstanceResult AddInstance(InstanceInfo instance) {
		if (string.IsNullOrWhiteSpace(instance.InstanceName)) {
			return AddInstanceResult.InstanceNameMustNotBeEmpty;
		}

		if (instance.MemoryAllocation.RawValue == 0) {
			return AddInstanceResult.InstanceMemoryMustNotBeZero;
		}
		
		lock (this) {
			var agentStats = Services.AgentManager.GetAgentStats(instance.AgentGuid);
			if (agentStats == null) {
				return AddInstanceResult.AgentNotFound;
			}
			
			if (agentStats.UsedInstances >= agentStats.AgentInfo.MaxInstances) {
				return AddInstanceResult.AgentInstanceLimitExceeded;
			}

			var availableMemory = agentStats.AvailableMemory;
			if (instance.MemoryAllocation > availableMemory) {
				return AddInstanceResult.AgentMemoryLimitExceeded;
			}

			if (!instances.TryAdd(instance)) {
				return AddInstanceResult.GuidAlreadyExists;
			}
			
			Logger.Information("Added instance \"{InstanceName}\" (GUID {InstanceGuid}) to agent \"{AgentName}\" (GUID {AgentGuid}).", instance.InstanceName, instance.InstanceGuid, agentStats.AgentInfo.Name, instance.AgentGuid);
			return AddInstanceResult.Success;
		}
	}

	private sealed class ObservableInstances : ObservableState<ImmutableArray<InstanceInfo>> {
		private readonly RwLockedDictionary<Guid, InstanceInfo> instances = new (LockRecursionPolicy.NoRecursion);

		public bool TryAdd(InstanceInfo instance) {
			if (instances.TryAdd(instance.InstanceGuid, instance)) {
				Update();
				return true;
			}
			else {
				return false;
			}
		}
		
		public ImmutableArray<InstanceInfo> GetInstances() {
			return instances.ValuesCopy;
		}

		protected override ImmutableArray<InstanceInfo> GetData() {
			return instances.ValuesCopy;
		}
	}
}
