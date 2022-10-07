﻿using System.Collections.Immutable;
using Phantom.Common.Data.Instance;
using Phantom.Common.Data.Replies;
using Phantom.Common.Logging;
using Phantom.Common.Messages.ToAgent;
using Phantom.Server.Services.Agents;
using Phantom.Utils.Collections;
using Phantom.Utils.Events;
using Serilog;

namespace Phantom.Server.Services.Instances;

public sealed class InstanceManager {
	private static readonly ILogger Logger = PhantomLogger.Create<InstanceManager>();

	private readonly ObservableInstances instances = new (PhantomLogger.Create<InstanceManager, ObservableInstances>());

	public EventSubscribers<ImmutableDictionary<Guid, Instance>> InstancesChanged => instances.Subs;

	private readonly CancellationToken cancellationToken;
	private readonly AgentManager agentManager;

	public InstanceManager(ServiceConfiguration configuration, AgentManager agentManager) {
		this.cancellationToken = configuration.CancellationToken;
		this.agentManager = agentManager;
	}

	public async Task<AddInstanceResult> AddInstance(InstanceConfiguration configuration) {
		var agent = agentManager.GetAgent(configuration.AgentGuid);
		if (agent == null) {
			return AddInstanceResult.AgentNotFound;
		}

		if (!instances.TryAdd(new Instance(configuration))) {
			return AddInstanceResult.InstanceAlreadyExists;
		}

		var agentName = agent.Name;

		var reply = (ConfigureInstanceResult?) await agentManager.SendMessageWithReply(configuration.AgentGuid, sequenceId => new ConfigureInstanceMessage(sequenceId, configuration), TimeSpan.FromSeconds(10));
		if (reply == ConfigureInstanceResult.Success) {
			Logger.Information("Added instance \"{InstanceName}\" (GUID {InstanceGuid}) to agent \"{AgentName}\".", configuration.InstanceName, configuration.InstanceGuid, agentName);
			return AddInstanceResult.Success;
		}
		else {
			instances.TryRemove(configuration.InstanceGuid);

			var result = reply switch {
				null                                          => AddInstanceResult.AgentCommunicationError,
				ConfigureInstanceResult.AgentShuttingDown     => AddInstanceResult.AgentShuttingDown,
				ConfigureInstanceResult.InstanceLimitExceeded => AddInstanceResult.AgentInstanceLimitExceeded,
				ConfigureInstanceResult.MemoryLimitExceeded   => AddInstanceResult.AgentMemoryLimitExceeded,
				_                                             => AddInstanceResult.UnknownError
			};

			Logger.Information("Failed adding instance \"{InstanceName}\" (GUID {InstanceGuid}) to agent \"{AgentName}\". {ErrorMessage}", configuration.InstanceName, configuration.InstanceGuid, agentName, result.ToSentence());
			return result;
		}
	}

	internal void SetInstanceState(Guid instanceGuid, InstanceStatus instanceStatus) {
		instances.Update(instanceGuid, instance => instance with { Status = instanceStatus });
	}

	internal void SetInstanceStatesForAgent(Guid agentGuid, InstanceStatus instanceStatus) {
		instances.UpdateAllForAgent(agentGuid, instance => instance with { Status = instanceStatus });
	}

	internal ImmutableArray<InstanceConfiguration> GetInstanceConfigurationsForAgent(Guid agentGuid) {
		return instances.GetInstances().Values.Select(static instance => instance.Configuration).Where(configuration => configuration.AgentGuid == agentGuid).ToImmutableArray();
	}

	private sealed class ObservableInstances : ObservableState<ImmutableDictionary<Guid, Instance>> {
		private readonly RwLockedDictionary<Guid, Instance> instances = new (LockRecursionPolicy.NoRecursion);

		public ObservableInstances(ILogger logger) : base(logger) {}

		public bool TryAdd(Instance configuration) {
			return UpdateIf(instances.TryAdd(configuration.Configuration.InstanceGuid, configuration));
		}

		public bool TryRemove(Guid instanceGuid) {
			return UpdateIf(instances.TryRemove(instanceGuid));
		}

		public void Update(Guid instanceGuid, Func<Instance, Instance> updater) {
			UpdateIf(instances.TryReplace(instanceGuid, updater));
		}

		public void UpdateAllForAgent(Guid agentGuid, Func<Instance, Instance> updater) {
			UpdateIf(instances.TryReplaceAllIf(updater, instance => instance.Configuration.AgentGuid == agentGuid));
		}

		public ImmutableDictionary<Guid, Instance> GetInstances() {
			return instances.ToImmutable();
		}

		protected override ImmutableDictionary<Guid, Instance> GetData() {
			return instances.ToImmutable();
		}
	}
}
