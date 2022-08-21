using System.Collections.Immutable;
using Phantom.Common.Data;
using Phantom.Common.Logging;
using Phantom.Common.Messages.ToAgent;
using Phantom.Utils.Collections;
using Phantom.Utils.Events;
using Serilog;

namespace Phantom.Server.Services.Instances;

public sealed class InstanceManager {
	private static readonly ILogger Logger = PhantomLogger.Create<InstanceManager>();

	private readonly ObservableInstances instances = new (PhantomLogger.Create<InstanceManager, ObservableInstances>());

	public EventSubscribers<ImmutableArray<InstanceInfo>> InstancesChanged => instances.Subs;

	internal InstanceManager() {}

	public async Task<AddInstanceResult> AddInstance(InstanceInfo instance) {
		if (string.IsNullOrWhiteSpace(instance.InstanceName)) {
			return AddInstanceResult.InstanceNameMustNotBeEmpty;
		}

		if (instance.MemoryAllocation.RawValue == 0) {
			return AddInstanceResult.InstanceMemoryMustNotBeZero;
		}

		string agentName;
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

			agentName = agentStats.AgentInfo.Name;
		}

		var reply = (CreateInstanceResult?) await Services.AgentManager.SendMessageWithReply(instance.AgentGuid, sequenceId => new CreateInstanceMessage(sequenceId, instance), TimeSpan.FromSeconds(10));
		if (reply == CreateInstanceResult.Success) {
			Logger.Information("Added instance \"{InstanceName}\" (GUID {InstanceGuid}) to agent \"{AgentName}\".", instance.InstanceName, instance.InstanceGuid, agentName);
			return AddInstanceResult.Success;
		}

		instances.TryRemove(instance.InstanceGuid);
		
		var (result, errorMessage) = reply switch {
			null => (AddInstanceResult.AgentCommunicationError, "Agent did not reply in time."),
			_    => (AddInstanceResult.UnknownError, "Unknown error.")
		};

		Logger.Information("Failed adding instance \"{InstanceName}\" (GUID {InstanceGuid}) to agent \"{AgentName}\". {ErrorMessage}", instance.InstanceName, instance.InstanceGuid, agentName, errorMessage);
		return result;
	}

	public InstanceInfo? GetInstance(Guid instanceGuid) {
		return instances.GetInstance(instanceGuid);
	}

	public async Task LaunchInstance(Guid instanceGuid) {
		var instanceInfo = GetInstance(instanceGuid);
		if (instanceInfo != null) {
			var reply = (SetInstanceStateResult?) await Services.AgentManager.SendMessageWithReply(instanceInfo.AgentGuid, sequenceId => new SetInstanceStateMessage(sequenceId, instanceGuid, IsRunning: true), TimeSpan.FromSeconds(10));
			if (reply == SetInstanceStateResult.Success) {
				// TODO
			}
		}
	}

	private sealed class ObservableInstances : ObservableState<ImmutableArray<InstanceInfo>> {
		private readonly RwLockedDictionary<Guid, InstanceInfo> instances = new (LockRecursionPolicy.NoRecursion);

		public ObservableInstances(ILogger logger) : base(logger) {}

		public bool TryAdd(InstanceInfo instance) {
			if (instances.TryAdd(instance.InstanceGuid, instance)) {
				Update();
				return true;
			}
			else {
				return false;
			}
		}

		public bool TryRemove(Guid instanceGuid) {
			if (instances.TryRemove(instanceGuid)) {
				Update();
				return true;
			}
			else {
				return false;
			}
		}

		public InstanceInfo? GetInstance(Guid instanceGuid) {
			return instances.TryGetValue(instanceGuid, out var instance) ? instance : null;
		}

		public ImmutableArray<InstanceInfo> GetInstances() {
			return instances.ValuesCopy;
		}

		protected override ImmutableArray<InstanceInfo> GetData() {
			return instances.ValuesCopy;
		}
	}
}
