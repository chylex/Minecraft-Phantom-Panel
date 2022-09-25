using System.Collections.Concurrent;
using System.Collections.Immutable;
using Phantom.Common.Data.Instance;
using Phantom.Common.Data.Replies;
using Phantom.Common.Logging;
using Phantom.Common.Messages.ToAgent;
using Phantom.Common.Messages.ToServer;
using Phantom.Server.Database;
using Phantom.Server.Services.Agents;
using Phantom.Utils.Collections;
using Phantom.Utils.Events;
using Serilog;

namespace Phantom.Server.Services.Instances;

public sealed class InstanceManager {
	private static readonly ILogger Logger = PhantomLogger.Create<InstanceManager>();

	private readonly ObservableInstances instances = new (PhantomLogger.Create<InstanceManager, ObservableInstances>());
	private readonly ConcurrentDictionary<Guid, ObservableInstanceLogs> instanceLogs = new ();

	public EventSubscribers<ImmutableArray<InstanceInfo>> InstancesChanged => instances.Subs;

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
			instances.AddOrReplace(instance.AsInstanceInfo);
		}
	}

	public async Task<AddInstanceResult> AddInstance(InstanceInfo instanceInfo) {
		var agent = agentManager.GetAgent(instanceInfo.AgentGuid);
		if (agent == null) {
			return AddInstanceResult.AgentNotFound;
		}

		if (!instances.TryAdd(instanceInfo)) {
			return AddInstanceResult.InstanceAlreadyExists;
		}

		using (var scope = databaseProvider.CreateScope()) {
			scope.Ctx.InstanceUpsert.Fetch(instanceInfo.InstanceGuid).SetFromInstanceInfo(instanceInfo);
			await scope.Ctx.SaveChangesAsync(cancellationToken);
		}

		var agentName = agent.Name;

		var reply = (ConfigureInstanceResult?) await agentManager.SendMessageWithReply(instanceInfo.AgentGuid, sequenceId => new ConfigureInstanceMessage(sequenceId, instanceInfo), TimeSpan.FromSeconds(10));
		if (reply == ConfigureInstanceResult.Success) {
			Logger.Information("Added instance \"{InstanceName}\" (GUID {InstanceGuid}) to agent \"{AgentName}\".", instanceInfo.InstanceName, instanceInfo.InstanceGuid, agentName);
			return AddInstanceResult.Success;
		}
		else {
			instances.TryRemove(instanceInfo.InstanceGuid);

			var (result, errorMessage) = reply switch {
				null => (AddInstanceResult.AgentCommunicationError, "Agent did not reply in time."),
				_    => (AddInstanceResult.UnknownError, "Unknown error.")
			};

			Logger.Information("Failed adding instance \"{InstanceName}\" (GUID {InstanceGuid}) to agent \"{AgentName}\". {ErrorMessage}", instanceInfo.InstanceName, instanceInfo.InstanceGuid, agentName, errorMessage);
			return result;
		}
	}

	public InstanceInfo? GetInstance(Guid instanceGuid) {
		return instances.GetInstance(instanceGuid);
	}

	public async Task<LaunchInstanceResult> LaunchInstance(Guid instanceGuid) {
		var instanceInfo = GetInstance(instanceGuid);
		if (instanceInfo == null) {
			return LaunchInstanceResult.InstanceDoesNotExist;
		}

		var reply = (LaunchInstanceResult?) await agentManager.SendMessageWithReply(instanceInfo.AgentGuid, sequenceId => new LaunchInstanceMessage(sequenceId, instanceGuid), TimeSpan.FromSeconds(10));
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

	internal ImmutableArray<InstanceInfo> GetInstancesForAgent(Guid agentGuid) {
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

	private sealed class ObservableInstances : ObservableState<ImmutableArray<InstanceInfo>> {
		private readonly RwLockedDictionary<Guid, InstanceInfo> instances = new (LockRecursionPolicy.NoRecursion);

		public ObservableInstances(ILogger logger) : base(logger) {}

		public void AddOrReplace(InstanceInfo instance) {
			instances[instance.InstanceGuid] = instance;
			Update();
		}

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
