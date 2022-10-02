using System.Collections.Immutable;
using Phantom.Common.Data.Instance;
using Phantom.Common.Data.Replies;
using Phantom.Common.Logging;
using Phantom.Common.Messages.ToAgent;
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

	public EventSubscribers<ImmutableArray<Instance>> InstancesChanged => instances.Subs;

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

		await foreach (var instance in scope.Ctx.Instances.AsAsyncEnumerable().WithCancellation(cancellationToken)) {
			var configuration = new InstanceConfiguration(
				instance.AgentGuid,
				instance.InstanceGuid,
				instance.InstanceName,
				instance.ServerPort,
				instance.RconPort,
				instance.MinecraftVersion,
				instance.MinecraftServerKind,
				instance.MemoryAllocation,
				instance.JavaRuntimeGuid
			);
			
			instances.AddOrReplace(new Instance(configuration));
		}
	}

	public async Task<AddInstanceResult> AddInstance(InstanceConfiguration configuration) {
		var agent = agentManager.GetAgent(configuration.AgentGuid);
		if (agent == null) {
			return AddInstanceResult.AgentNotFound;
		}

		if (!instances.TryAdd(new Instance(configuration))) {
			return AddInstanceResult.InstanceAlreadyExists;
		}

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
			
			await scope.Ctx.SaveChangesAsync(cancellationToken);
		}

		var agentName = agent.Name;

		var reply = (ConfigureInstanceResult?) await agentManager.SendMessageWithReply(configuration.AgentGuid, sequenceId => new ConfigureInstanceMessage(sequenceId, configuration), TimeSpan.FromSeconds(10));
		if (reply == ConfigureInstanceResult.Success) {
			Logger.Information("Added instance \"{InstanceName}\" (GUID {InstanceGuid}) to agent \"{AgentName}\".", configuration.InstanceName, configuration.InstanceGuid, agentName);
			return AddInstanceResult.Success;
		}
		else {
			instances.TryRemove(configuration.InstanceGuid);

			var (result, errorMessage) = reply switch {
				null => (AddInstanceResult.AgentCommunicationError, "Agent did not reply in time."),
				_    => (AddInstanceResult.UnknownError, "Unknown error.")
			};

			Logger.Information("Failed adding instance \"{InstanceName}\" (GUID {InstanceGuid}) to agent \"{AgentName}\". {ErrorMessage}", configuration.InstanceName, configuration.InstanceGuid, agentName, errorMessage);
			return result;
		}
	}

	public Instance? GetInstance(Guid instanceGuid) {
		return instances.GetInstance(instanceGuid);
	}

	public void SetInstanceState(Guid instanceGuid, InstanceStatus instanceStatus) {
		instances.Update(instanceGuid, instance => instance with { Status = instanceStatus });
	}

	public async Task<LaunchInstanceResult> LaunchInstance(Guid instanceGuid) {
		var instance = GetInstance(instanceGuid);
		if (instance == null) {
			return LaunchInstanceResult.InstanceDoesNotExist;
		}

		var reply = (LaunchInstanceResult?) await agentManager.SendMessageWithReply(instance.Configuration.AgentGuid, sequenceId => new LaunchInstanceMessage(sequenceId, instanceGuid), TimeSpan.FromSeconds(300));
		return reply ?? LaunchInstanceResult.CommunicationError;
	}

	public async Task<SendCommandToInstanceResult> SendCommand(Guid instanceGuid, string command) {
		var instance = GetInstance(instanceGuid);
		if (instance != null) {
			var reply = (SendCommandToInstanceResult?) await agentManager.SendMessageWithReply(instance.Configuration.AgentGuid, sequenceId => new SendCommandToInstanceMessage(sequenceId, instanceGuid, command), TimeSpan.FromSeconds(10));
			if (reply == SendCommandToInstanceResult.Success) {
				// TODO
			}

			return reply ?? SendCommandToInstanceResult.UnknownError;
		}

		return SendCommandToInstanceResult.InstanceDoesNotExist;
	}

	internal ImmutableArray<InstanceConfiguration> GetInstanceConfigurationsForAgent(Guid agentGuid) {
		return instances.GetInstances().Select(static instance => instance.Configuration).Where(configuration => configuration.AgentGuid == agentGuid).ToImmutableArray();
	}

	private sealed class ObservableInstances : ObservableState<ImmutableArray<Instance>> {
		private readonly RwLockedDictionary<Guid, Instance> instances = new (LockRecursionPolicy.NoRecursion);

		public ObservableInstances(ILogger logger) : base(logger) {}

		public void AddOrReplace(Instance configuration) {
			instances[configuration.Configuration.InstanceGuid] = configuration;
			Update();
		}

		public bool TryAdd(Instance configuration) {
			if (instances.TryAdd(configuration.Configuration.InstanceGuid, configuration)) {
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

		public void Update(Guid guid, Func<Instance, Instance> updater) {
			if (instances.TryReplace(guid, updater)) {
				Update();
			}
		}

		public Instance? GetInstance(Guid instanceGuid) {
			return instances.TryGetValue(instanceGuid, out var instance) ? instance : null;
		}

		public ImmutableArray<Instance> GetInstances() {
			return instances.ValuesCopy;
		}

		protected override ImmutableArray<Instance> GetData() {
			return instances.ValuesCopy;
		}
	}
}
