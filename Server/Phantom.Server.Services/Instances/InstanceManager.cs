using System.Collections.Immutable;
using Phantom.Common.Data.Instance;
using Phantom.Common.Data.Minecraft;
using Phantom.Common.Data.Replies;
using Phantom.Common.Logging;
using Phantom.Common.Messages.ToAgent;
using Phantom.Common.Minecraft;
using Phantom.Server.Database;
using Phantom.Server.Database.Entities;
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
	private readonly DatabaseProvider databaseProvider;

	public InstanceManager(ServiceConfiguration configuration, AgentManager agentManager, DatabaseProvider databaseProvider) {
		this.cancellationToken = configuration.CancellationToken;
		this.agentManager = agentManager;
		this.databaseProvider = databaseProvider;
	}

	public async Task Initialize() {
		using var scope = databaseProvider.CreateScope();

		await foreach (var entity in scope.Ctx.Instances.AsAsyncEnumerable().WithCancellation(cancellationToken)) {
			var configuration = new InstanceConfiguration(
				entity.AgentGuid,
				entity.InstanceGuid,
				entity.InstanceName,
				entity.ServerPort,
				entity.RconPort,
				entity.MinecraftVersion,
				entity.MinecraftServerKind,
				entity.MemoryAllocation,
				entity.JavaRuntimeGuid,
				JvmArgumentsHelper.Split(entity.JvmArguments),
				entity.LaunchAutomatically
			);

			var instance = new Instance(configuration);
			instances.ByGuid[instance.Configuration.InstanceGuid] = instance;
		}
	}

	public async Task<AddInstanceResult> AddInstance(InstanceConfiguration configuration) {
		var agent = agentManager.GetAgent(configuration.AgentGuid);
		if (agent == null) {
			return AddInstanceResult.AgentNotFound;
		}

		var instance = new Instance(configuration);
		if (!instances.ByGuid.TryAdd(instance.Configuration.InstanceGuid, instance)) {
			return AddInstanceResult.InstanceAlreadyExists;
		}

		var agentName = agent.Name;

		var reply = (ConfigureInstanceResult?) await agentManager.SendMessageWithReply(configuration.AgentGuid, sequenceId => new ConfigureInstanceMessage(sequenceId, configuration), TimeSpan.FromSeconds(10));
		if (reply == ConfigureInstanceResult.Success) {
			using (var scope = databaseProvider.CreateScope()) {
				InstanceEntity entity = scope.Ctx.InstanceUpsert.Fetch(configuration.InstanceGuid);

				entity.AgentGuid = configuration.AgentGuid;
				entity.InstanceName = configuration.InstanceName;
				entity.ServerPort = configuration.ServerPort;
				entity.RconPort = configuration.RconPort;
				entity.MinecraftVersion = configuration.MinecraftVersion;
				entity.MinecraftServerKind = configuration.MinecraftServerKind;
				entity.MemoryAllocation = configuration.MemoryAllocation;
				entity.JavaRuntimeGuid = configuration.JavaRuntimeGuid;
				entity.JvmArguments = JvmArgumentsHelper.Join(configuration.JvmArguments);
				entity.LaunchAutomatically = configuration.LaunchAutomatically;

				await scope.Ctx.SaveChangesAsync(cancellationToken);
			}

			Logger.Information("Added instance \"{InstanceName}\" (GUID {InstanceGuid}) to agent \"{AgentName}\".", configuration.InstanceName, configuration.InstanceGuid, agentName);
			return AddInstanceResult.Success;
		}
		else {
			instances.ByGuid.Remove(configuration.InstanceGuid);

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

	private Instance? GetInstance(Guid instanceGuid) {
		return instances.ByGuid.TryGetValue(instanceGuid, out var instance) ? instance : null;
	}

	internal void SetInstanceState(Guid instanceGuid, InstanceStatus instanceStatus) {
		instances.ByGuid.TryReplace(instanceGuid, instance => instance with { Status = instanceStatus });
	}

	internal void SetInstanceStatesForAgent(Guid agentGuid, InstanceStatus instanceStatus) {
		instances.ByGuid.ReplaceAllIf(instance => instance with { Status = instanceStatus }, instance => instance.Configuration.AgentGuid == agentGuid);
	}

	public async Task<LaunchInstanceResult> LaunchInstance(Guid instanceGuid) {
		var instance = GetInstance(instanceGuid);
		if (instance == null) {
			return LaunchInstanceResult.InstanceDoesNotExist;
		}

		await SetInstanceShouldLaunchAutomatically(instanceGuid, true);

		var reply = (LaunchInstanceResult?) await agentManager.SendMessageWithReply(instance.Configuration.AgentGuid, sequenceId => new LaunchInstanceMessage(sequenceId, instanceGuid), TimeSpan.FromSeconds(10));
		return reply ?? LaunchInstanceResult.CommunicationError;
	}

	public async Task<StopInstanceResult> StopInstance(Guid instanceGuid, MinecraftStopStrategy stopStrategy) {
		var instance = GetInstance(instanceGuid);
		if (instance == null) {
			return StopInstanceResult.InstanceDoesNotExist;
		}

		await SetInstanceShouldLaunchAutomatically(instanceGuid, false);

		var reply = (StopInstanceResult?) await agentManager.SendMessageWithReply(instance.Configuration.AgentGuid, sequenceId => new StopInstanceMessage(sequenceId, instanceGuid, stopStrategy), TimeSpan.FromSeconds(10));
		return reply ?? StopInstanceResult.CommunicationError;
	}

	private async Task SetInstanceShouldLaunchAutomatically(Guid instanceGuid, bool shouldLaunchAutomatically) {
		instances.ByGuid.TryReplace(instanceGuid, instance => instance with {
			Configuration = instance.Configuration with { LaunchAutomatically = shouldLaunchAutomatically }
		});

		using var scope = databaseProvider.CreateScope();
		var entity = await scope.Ctx.Instances.FindAsync(instanceGuid, cancellationToken);
		if (entity != null) {
			entity.LaunchAutomatically = shouldLaunchAutomatically;
			await scope.Ctx.SaveChangesAsync(cancellationToken);
		}
	}

	public async Task<SendCommandToInstanceResult> SendCommand(Guid instanceGuid, string command) {
		var instance = GetInstance(instanceGuid);
		if (instance != null) {
			var reply = (SendCommandToInstanceResult?) await agentManager.SendMessageWithReply(instance.Configuration.AgentGuid, sequenceId => new SendCommandToInstanceMessage(sequenceId, instanceGuid, command), TimeSpan.FromSeconds(10));
			return reply ?? SendCommandToInstanceResult.AgentCommunicationError;
		}

		return SendCommandToInstanceResult.InstanceDoesNotExist;
	}

	internal ImmutableArray<InstanceConfiguration> GetInstanceConfigurationsForAgent(Guid agentGuid) {
		return instances.ByGuid.ValuesCopy.Select(static instance => instance.Configuration).Where(configuration => configuration.AgentGuid == agentGuid).ToImmutableArray();
	}

	private sealed class ObservableInstances : ObservableState<ImmutableDictionary<Guid, Instance>> {
		public RwLockedObservableDictionary<Guid, Instance> ByGuid { get; } = new (LockRecursionPolicy.NoRecursion);

		public ObservableInstances(ILogger logger) : base(logger) {
			ByGuid.CollectionChanged += Update;
		}

		protected override ImmutableDictionary<Guid, Instance> GetData() {
			return ByGuid.ToImmutable();
		}
	}
}
