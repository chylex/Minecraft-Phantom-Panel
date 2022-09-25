using System.Collections.Concurrent;
using System.Collections.Immutable;
using Phantom.Common.Data.Instance;
using Phantom.Common.Data.Replies;
using Phantom.Common.Logging;
using Phantom.Common.Messages.ToAgent;
using Phantom.Common.Messages.ToServer;
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
	private readonly ConcurrentDictionary<Guid, ObservableInstanceLogs> instanceLogs = new ();

	public EventSubscribers<ImmutableArray<InstanceConfiguration>> InstancesChanged => instances.Subs;

	private readonly CancellationToken cancellationToken;
	private readonly AgentManager agentManager;
	private readonly DatabaseProvider databaseProvider;
	private readonly IServiceProvider serviceProvider;

	public InstanceManager(ServiceConfiguration configuration, AgentManager agentManager, DatabaseProvider databaseProvider, IServiceProvider serviceProvider) {
		this.cancellationToken = configuration.CancellationToken;
		this.agentManager = agentManager;
		this.databaseProvider = databaseProvider;
		this.serviceProvider = serviceProvider;
	}

	public async Task Initialize() {
		using var scope = databaseProvider.CreateScope();

		await foreach (var instance in scope.Ctx.Instances.AsAsyncEnumerable().WithCancellation(cancellationToken)) {
			var instanceConfiguration = new InstanceConfiguration(
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
			
			instances.AddOrReplace(instanceConfiguration);
		}
	}

	public async Task<AddInstanceResult> AddInstance(InstanceConfiguration instanceConfiguration) {
		var agent = agentManager.GetAgent(instanceConfiguration.AgentGuid);
		if (agent == null) {
			return AddInstanceResult.AgentNotFound;
		}

		if (!instances.TryAdd(instanceConfiguration)) {
			return AddInstanceResult.InstanceAlreadyExists;
		}

		using (var scope = databaseProvider.CreateScope()) {
			InstanceEntity entity = scope.Ctx.InstanceUpsert.Fetch(instanceConfiguration.InstanceGuid);
			
			entity.AgentGuid = instanceConfiguration.AgentGuid;
			entity.InstanceName = instanceConfiguration.InstanceName;
			entity.ServerPort = instanceConfiguration.ServerPort;
			entity.RconPort = instanceConfiguration.RconPort;
			entity.MinecraftVersion = instanceConfiguration.MinecraftVersion;
			entity.MinecraftServerKind = instanceConfiguration.MinecraftServerKind;
			entity.MemoryAllocation = instanceConfiguration.MemoryAllocation;
			entity.JavaRuntimeGuid = instanceConfiguration.JavaRuntimeGuid;
			
			await scope.Ctx.SaveChangesAsync(cancellationToken);
		}

		var agentName = agent.Name;

		var reply = (ConfigureInstanceResult?) await agentManager.SendMessageWithReply(instanceConfiguration.AgentGuid, sequenceId => new ConfigureInstanceMessage(sequenceId, instanceConfiguration), TimeSpan.FromSeconds(10));
		if (reply == ConfigureInstanceResult.Success) {
			Logger.Information("Added instance \"{InstanceName}\" (GUID {InstanceGuid}) to agent \"{AgentName}\".", instanceConfiguration.InstanceName, instanceConfiguration.InstanceGuid, agentName);
			return AddInstanceResult.Success;
		}
		else {
			instances.TryRemove(instanceConfiguration.InstanceGuid);

			var (result, errorMessage) = reply switch {
				null => (AddInstanceResult.AgentCommunicationError, "Agent did not reply in time."),
				_    => (AddInstanceResult.UnknownError, "Unknown error.")
			};

			Logger.Information("Failed adding instance \"{InstanceName}\" (GUID {InstanceGuid}) to agent \"{AgentName}\". {ErrorMessage}", instanceConfiguration.InstanceName, instanceConfiguration.InstanceGuid, agentName, errorMessage);
			return result;
		}
	}

	public InstanceConfiguration? GetInstance(Guid instanceGuid) {
		return instances.GetInstance(instanceGuid);
	}

	public async Task<LaunchInstanceResult> LaunchInstance(Guid instanceGuid) {
		var instanceInfo = GetInstance(instanceGuid);
		if (instanceInfo == null) {
			return LaunchInstanceResult.InstanceDoesNotExist;
		}

		var reply = (LaunchInstanceResult?) await agentManager.SendMessageWithReply(instanceInfo.AgentGuid, sequenceId => new LaunchInstanceMessage(sequenceId, instanceGuid), TimeSpan.FromSeconds(300));
		return reply ?? LaunchInstanceResult.CommunicationError;
	}

	public async Task<SendCommandToInstanceResult> SendCommand(Guid instanceGuid, string command) {
		var instanceInfo = GetInstance(instanceGuid);
		if (instanceInfo != null) {
			var reply = (SendCommandToInstanceResult?) await agentManager.SendMessageWithReply(instanceInfo.AgentGuid, sequenceId => new SendCommandToInstanceMessage(sequenceId, instanceGuid, command), TimeSpan.FromSeconds(10));
			if (reply == SendCommandToInstanceResult.Success) {
				// TODO
			}

			return reply ?? SendCommandToInstanceResult.UnknownError;
		}

		return SendCommandToInstanceResult.InstanceDoesNotExist;
	}

	internal ImmutableArray<InstanceConfiguration> GetInstancesForAgent(Guid agentGuid) {
		return instances.GetInstances().Where(instance => instance.AgentGuid == agentGuid).ToImmutableArray();
	}

	private ObservableInstanceLogs GetInstanceLogs(Guid instanceGuid) {
		return instanceLogs.GetOrAdd(instanceGuid, static _ => new ObservableInstanceLogs(PhantomLogger.Create<InstanceManager, ObservableInstanceLogs>()));
	}

	internal void AddInstanceLogs(InstanceOutputMessage message) {
		GetInstanceLogs(message.InstanceGuid).Add(message.Lines);
	}

	public EventSubscribers<RingBuffer<string>> GetInstanceLogsSubs(Guid instanceGuid) {
		return GetInstanceLogs(instanceGuid).Subs;
	}

	private sealed class ObservableInstances : ObservableState<ImmutableArray<InstanceConfiguration>> {
		private readonly RwLockedDictionary<Guid, InstanceConfiguration> instances = new (LockRecursionPolicy.NoRecursion);

		public ObservableInstances(ILogger logger) : base(logger) {}

		public void AddOrReplace(InstanceConfiguration configuration) {
			instances[configuration.InstanceGuid] = configuration;
			Update();
		}

		public bool TryAdd(InstanceConfiguration configuration) {
			if (instances.TryAdd(configuration.InstanceGuid, configuration)) {
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

		public InstanceConfiguration? GetInstance(Guid instanceGuid) {
			return instances.TryGetValue(instanceGuid, out var instance) ? instance : null;
		}

		public ImmutableArray<InstanceConfiguration> GetInstances() {
			return instances.ValuesCopy;
		}

		protected override ImmutableArray<InstanceConfiguration> GetData() {
			return instances.ValuesCopy;
		}
	}

	private sealed class ObservableInstanceLogs : ObservableState<RingBuffer<string>> {
		private readonly RingBuffer<string> log = new (1000);

		public ObservableInstanceLogs(ILogger logger) : base(logger) {}

		public void Add(ImmutableArray<string> lines) {
			foreach (var line in lines) {
				log.Add(line);
			}

			Update();
		}

		protected override RingBuffer<string> GetData() {
			return log;
		}
	}
}
