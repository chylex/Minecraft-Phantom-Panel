using System.Collections.Immutable;
using Akka.Actor;
using Microsoft.EntityFrameworkCore;
using Phantom.Common.Data;
using Phantom.Common.Data.Agent;
using Phantom.Common.Data.Instance;
using Phantom.Common.Data.Java;
using Phantom.Common.Data.Minecraft;
using Phantom.Common.Data.Replies;
using Phantom.Common.Data.Web.Agent;
using Phantom.Common.Data.Web.Instance;
using Phantom.Common.Data.Web.Minecraft;
using Phantom.Common.Messages.Agent;
using Phantom.Common.Messages.Agent.ToAgent;
using Phantom.Controller.Database;
using Phantom.Controller.Minecraft;
using Phantom.Controller.Services.Instances;
using Phantom.Utils.Actor;
using Phantom.Utils.Actor.Mailbox;
using Phantom.Utils.Actor.Tasks;
using Phantom.Utils.Logging;
using Phantom.Utils.Rpc.Runtime;
using Serilog;

namespace Phantom.Controller.Services.Agents;

sealed class AgentActor : ReceiveActor<AgentActor.ICommand> {
	private static readonly ILogger Logger = PhantomLogger.Create<AgentActor>();

	private static readonly TimeSpan DisconnectionRecheckInterval = TimeSpan.FromSeconds(5);
	private static readonly TimeSpan DisconnectionThreshold = TimeSpan.FromSeconds(12);

	public readonly record struct Init(Guid AgentGuid, AgentConfiguration AgentConfiguration, ControllerState ControllerState, MinecraftVersions MinecraftVersions, IDbContextProvider DbProvider, CancellationToken CancellationToken);
	
	public static Props<ICommand> Factory(Init init) {
		return Props<ICommand>.Create(() => new AgentActor(init), new ActorConfiguration { SupervisorStrategy = SupervisorStrategies.Resume, MailboxType = UnboundedJumpAheadMailbox.Name });
	}

	private readonly ControllerState controllerState;
	private readonly MinecraftVersions minecraftVersions;
	private readonly IDbContextProvider dbProvider;
	private readonly CancellationToken cancellationToken;
	
	private readonly Guid agentGuid;
	
	private AgentConfiguration configuration;
	private AgentStats? stats;
	private ImmutableArray<TaggedJavaRuntime> javaRuntimes = ImmutableArray<TaggedJavaRuntime>.Empty;
	
	private readonly AgentConnection connection;
	
	private DateTimeOffset? lastPingTime;
	private bool isOnline;

	private IAgentConnectionStatus ConnectionStatus {
		get {
			if (isOnline) {
				return AgentConnectionStatus.Online;
			}
			else if (lastPingTime == null) {
				return AgentConnectionStatus.Offline;
			}
			else {
				return AgentConnectionStatus.Disconnected(lastPingTime.Value);
			}
		}
	}

	private readonly ActorRef<AgentDatabaseStorageActor.ICommand> databaseStorageActor;
	
	private readonly Dictionary<Guid, ActorRef<InstanceActor.ICommand>> instanceActorByGuid = new ();
	private readonly Dictionary<Guid, Instance> instanceDataByGuid = new ();

	private AgentActor(Init init) {
		this.controllerState = init.ControllerState;
		this.minecraftVersions = init.MinecraftVersions;
		this.dbProvider = init.DbProvider;
		this.cancellationToken = init.CancellationToken;
		
		this.agentGuid = init.AgentGuid;
		this.configuration = init.AgentConfiguration;
		this.connection = new AgentConnection(agentGuid, configuration.AgentName);
		
		this.databaseStorageActor = Context.ActorOf(AgentDatabaseStorageActor.Factory(new AgentDatabaseStorageActor.Init(agentGuid, init.DbProvider, init.CancellationToken)), "DatabaseStorage");

		NotifyAgentUpdated();
		
		ReceiveAsync<InitializeCommand>(Initialize);
		ReceiveAsyncAndReply<RegisterCommand, ImmutableArray<ConfigureInstanceMessage>>(Register);
		Receive<UnregisterCommand>(Unregister);
		Receive<RefreshConnectionStatusCommand>(RefreshConnectionStatus);
		Receive<NotifyIsAliveCommand>(NotifyIsAlive);
		Receive<UpdateStatsCommand>(UpdateStats);
		Receive<UpdateJavaRuntimesCommand>(UpdateJavaRuntimes);
		ReceiveAndReplyLater<CreateOrUpdateInstanceCommand, InstanceActionResult<CreateOrUpdateInstanceResult>>(CreateOrUpdateInstance);
		Receive<UpdateInstanceStatusCommand>(UpdateInstanceStatus);
		ReceiveAndReplyLater<LaunchInstanceCommand, InstanceActionResult<LaunchInstanceResult>>(LaunchInstance);
		ReceiveAndReplyLater<StopInstanceCommand, InstanceActionResult<StopInstanceResult>>(StopInstance);
		ReceiveAndReplyLater<SendCommandToInstanceCommand, InstanceActionResult<SendCommandToInstanceResult>>(SendMinecraftCommand);
		Receive<ReceiveInstanceDataCommand>(ReceiveInstanceData);
	}

	private void NotifyAgentUpdated() {
		controllerState.UpdateAgent(new Agent(agentGuid, configuration, stats, ConnectionStatus));
	}

	protected override void PreStart() {
		Self.Tell(new InitializeCommand());
		
		Context.System.Scheduler.ScheduleTellRepeatedly(DisconnectionRecheckInterval, DisconnectionRecheckInterval, Self, new RefreshConnectionStatusCommand(), Self);
	}

	private ActorRef<InstanceActor.ICommand> CreateNewInstance(Instance instance) {
		UpdateInstanceData(instance);
		
		var instanceActor = CreateInstanceActor(instance);
		instanceActorByGuid.Add(instance.InstanceGuid, instanceActor);
		return instanceActor;
	}

	private void UpdateInstanceData(Instance instance) {
		instanceDataByGuid[instance.InstanceGuid] = instance;
		controllerState.UpdateInstance(instance);
	}

	private ActorRef<InstanceActor.ICommand> CreateInstanceActor(Instance instance) {
		var init = new InstanceActor.Init(instance, SelfTyped, connection, dbProvider, cancellationToken);
		var name = "Instance:" + instance.InstanceGuid;
		return Context.ActorOf(InstanceActor.Factory(init), name);
	}

	private void TellInstance(Guid instanceGuid, InstanceActor.ICommand command) {
		if (instanceActorByGuid.TryGetValue(instanceGuid, out var instance)) {
			instance.Tell(command);
		}
		else {
			Logger.Warning("Could not deliver command {CommandType} to instance {InstanceGuid}, instance not found.", command.GetType().Name, instanceGuid);
		}
	}

	private void TellAllInstances(InstanceActor.ICommand command) {
		foreach (var instance in instanceActorByGuid.Values) {
			instance.Tell(command);
		}
	}

	private Task<InstanceActionResult<TReply>> RequestInstance<TCommand, TReply>(Guid instanceGuid, TCommand command) where TCommand : InstanceActor.ICommand, ICanReply<InstanceActionResult<TReply>> {
		if (instanceActorByGuid.TryGetValue(instanceGuid, out var instance)) {
			return instance.Request(command, cancellationToken);
		}
		else {
			Logger.Warning("Could not deliver command {CommandType} to instance {InstanceGuid}, instance not found.", command.GetType().Name, instanceGuid);
			return Task.FromResult(InstanceActionResult.General<TReply>(InstanceActionGeneralResult.InstanceDoesNotExist));
		}
	}

	private async Task<ImmutableArray<ConfigureInstanceMessage>> PrepareInitialConfigurationMessages() {
		var configurationMessages = ImmutableArray.CreateBuilder<ConfigureInstanceMessage>();
		
		foreach (var (instanceGuid, instanceConfiguration, _, launchAutomatically) in instanceDataByGuid.Values.ToImmutableArray()) {
			var serverExecutableInfo = await minecraftVersions.GetServerExecutableInfo(instanceConfiguration.MinecraftVersion, cancellationToken);
			configurationMessages.Add(new ConfigureInstanceMessage(instanceGuid, instanceConfiguration, new InstanceLaunchProperties(serverExecutableInfo), launchAutomatically));
		}

		return configurationMessages.ToImmutable();
	}

	public interface ICommand {}
	
	private sealed record InitializeCommand : ICommand;
	
	public sealed record RegisterCommand(AgentConfiguration Configuration, RpcConnectionToClient<IMessageToAgentListener> Connection) : ICommand, ICanReply<ImmutableArray<ConfigureInstanceMessage>>;
	
	public sealed record UnregisterCommand(RpcConnectionToClient<IMessageToAgentListener> Connection) : ICommand;
	
	private sealed record RefreshConnectionStatusCommand : ICommand;
	
	public sealed record NotifyIsAliveCommand : ICommand;
	
	public sealed record UpdateStatsCommand(int RunningInstanceCount, RamAllocationUnits RunningInstanceMemory) : ICommand;
	
	public sealed record UpdateJavaRuntimesCommand(ImmutableArray<TaggedJavaRuntime> JavaRuntimes) : ICommand;
	
	public sealed record CreateOrUpdateInstanceCommand(Guid AuditLogUserGuid, Guid InstanceGuid, InstanceConfiguration Configuration) : ICommand, ICanReply<InstanceActionResult<CreateOrUpdateInstanceResult>>;
	
	public sealed record UpdateInstanceStatusCommand(Guid InstanceGuid, IInstanceStatus Status) : ICommand;

	public sealed record LaunchInstanceCommand(Guid InstanceGuid, Guid AuditLogUserGuid) : ICommand, ICanReply<InstanceActionResult<LaunchInstanceResult>>;
	
	public sealed record StopInstanceCommand(Guid InstanceGuid, Guid AuditLogUserGuid, MinecraftStopStrategy StopStrategy) : ICommand, ICanReply<InstanceActionResult<StopInstanceResult>>;
	
	public sealed record SendCommandToInstanceCommand(Guid InstanceGuid, Guid AuditLogUserGuid, string Command) : ICommand, ICanReply<InstanceActionResult<SendCommandToInstanceResult>>;
	
	public sealed record ReceiveInstanceDataCommand(Instance Instance) : ICommand, IJumpAhead;

	private async Task Initialize(InitializeCommand command) {
		await using var ctx = dbProvider.Eager();
		await foreach (var entity in ctx.Instances.Where(instance => instance.AgentGuid == agentGuid).AsAsyncEnumerable().WithCancellation(cancellationToken)) {
			var instanceConfiguration = new InstanceConfiguration(
				entity.AgentGuid,
				entity.InstanceName,
				entity.ServerPort,
				entity.RconPort,
				entity.MinecraftVersion,
				entity.MinecraftServerKind,
				entity.MemoryAllocation,
				entity.JavaRuntimeGuid,
				JvmArgumentsHelper.Split(entity.JvmArguments)
			);

			CreateNewInstance(Instance.Offline(entity.InstanceGuid, instanceConfiguration, entity.LaunchAutomatically));
		}
	}

	private async Task<ImmutableArray<ConfigureInstanceMessage>> Register(RegisterCommand command) {
		var configurationMessages = await PrepareInitialConfigurationMessages();
		
		configuration = command.Configuration;
		connection.UpdateConnection(command.Connection, configuration.AgentName);
		
		lastPingTime = DateTimeOffset.Now;
		isOnline = true;
		NotifyAgentUpdated();
		
		Logger.Information("Registered agent \"{Name}\" (GUID {Guid}).", configuration.AgentName, agentGuid);
		
		databaseStorageActor.Tell(new AgentDatabaseStorageActor.StoreAgentConfigurationCommand(configuration));
		
		return configurationMessages;
	}

	private void Unregister(UnregisterCommand command) {
		if (connection.CloseIfSame(command.Connection)) {
			stats = null;
			lastPingTime = null;
			isOnline = false;
			NotifyAgentUpdated();
			
			TellAllInstances(new InstanceActor.SetStatusCommand(InstanceStatus.Offline));
			
			Logger.Information("Unregistered agent \"{Name}\" (GUID {Guid}).", configuration.AgentName, agentGuid);
		}
	}

	private void RefreshConnectionStatus(RefreshConnectionStatusCommand command) {
		if (isOnline && lastPingTime != null && DateTimeOffset.Now - lastPingTime >= DisconnectionThreshold) {
			isOnline = false;
			NotifyAgentUpdated();
			
			Logger.Warning("Lost connection to agent \"{Name}\" (GUID {Guid}).", configuration.AgentName, agentGuid);
		}
	}
	
	private void NotifyIsAlive(NotifyIsAliveCommand command) {
		lastPingTime = DateTimeOffset.Now;
		
		if (!isOnline) {
			isOnline = true;
			NotifyAgentUpdated();
		}
	}

	private void UpdateStats(UpdateStatsCommand command) {
		stats = new AgentStats(command.RunningInstanceCount, command.RunningInstanceMemory);
		NotifyAgentUpdated();
	}

	private void UpdateJavaRuntimes(UpdateJavaRuntimesCommand command) {
		javaRuntimes = command.JavaRuntimes;
		controllerState.UpdateAgentJavaRuntimes(agentGuid, javaRuntimes);
	}
	
	private Task<InstanceActionResult<CreateOrUpdateInstanceResult>> CreateOrUpdateInstance(CreateOrUpdateInstanceCommand command) {
		var instanceConfiguration = command.Configuration;

		if (string.IsNullOrWhiteSpace(instanceConfiguration.InstanceName)) {
			return Task.FromResult(InstanceActionResult.Concrete(CreateOrUpdateInstanceResult.InstanceNameMustNotBeEmpty));
		}
		
		if (instanceConfiguration.MemoryAllocation <= RamAllocationUnits.Zero) {
			return Task.FromResult(InstanceActionResult.Concrete(CreateOrUpdateInstanceResult.InstanceMemoryMustNotBeZero));
		}
		
		return minecraftVersions.GetServerExecutableInfo(instanceConfiguration.MinecraftVersion, cancellationToken)
		                        .ContinueOnActor(CreateOrUpdateInstance1, command)
		                        .Unwrap();
	}

	private Task<InstanceActionResult<CreateOrUpdateInstanceResult>> CreateOrUpdateInstance1(FileDownloadInfo? serverExecutableInfo, CreateOrUpdateInstanceCommand command) {
		if (serverExecutableInfo == null) {
			return Task.FromResult(InstanceActionResult.Concrete(CreateOrUpdateInstanceResult.MinecraftVersionDownloadInfoNotFound));
		}
		
		var instanceConfiguration = command.Configuration;
		
		bool isCreatingInstance = !instanceActorByGuid.TryGetValue(command.InstanceGuid, out var instanceActorRef);
		if (isCreatingInstance) {
			instanceActorRef = CreateNewInstance(Instance.Offline(command.InstanceGuid, instanceConfiguration));
		}
		
		var configureInstanceCommand = new InstanceActor.ConfigureInstanceCommand(command.AuditLogUserGuid, command.InstanceGuid, instanceConfiguration, new InstanceLaunchProperties(serverExecutableInfo), isCreatingInstance);
		
		return instanceActorRef.Request(configureInstanceCommand, cancellationToken)
		                       .ContinueOnActor(CreateOrUpdateInstance2, configureInstanceCommand);
	}
	
	private InstanceActionResult<CreateOrUpdateInstanceResult> CreateOrUpdateInstance2(InstanceActionResult<ConfigureInstanceResult> result, InstanceActor.ConfigureInstanceCommand command) {
		var instanceGuid = command.InstanceGuid;
		var instanceName = command.Configuration.InstanceName;
		var isCreating = command.IsCreatingInstance;

		if (result.Is(ConfigureInstanceResult.Success)) {
			string action = isCreating ? "Added" : "Edited";
			string relation = isCreating ? "to agent" : "in agent";
			Logger.Information(action + " instance \"{InstanceName}\" (GUID {InstanceGuid}) " + relation + " \"{AgentName}\".", instanceName, instanceGuid, configuration.AgentName);
		}
		else {
			string action = isCreating ? "adding" : "editing";
			string relation = isCreating ? "to agent" : "in agent";
			Logger.Information("Failed " + action + " instance \"{InstanceName}\" (GUID {InstanceGuid}) " + relation + " \"{AgentName}\". {ErrorMessage}", instanceName, instanceGuid, configuration.AgentName, result.ToSentence(ConfigureInstanceResultExtensions.ToSentence));
		}
		
		return result.Map(static result => result switch {
			ConfigureInstanceResult.Success => CreateOrUpdateInstanceResult.Success,
			_                               => CreateOrUpdateInstanceResult.UnknownError
		});
	}
	
	private void UpdateInstanceStatus(UpdateInstanceStatusCommand command) {
		TellInstance(command.InstanceGuid, new InstanceActor.SetStatusCommand(command.Status));
	}

	private Task<InstanceActionResult<LaunchInstanceResult>> LaunchInstance(LaunchInstanceCommand command) {
		return RequestInstance<InstanceActor.LaunchInstanceCommand, LaunchInstanceResult>(command.InstanceGuid, new InstanceActor.LaunchInstanceCommand(command.AuditLogUserGuid));
	}

	private Task<InstanceActionResult<StopInstanceResult>> StopInstance(StopInstanceCommand command) {
		return RequestInstance<InstanceActor.StopInstanceCommand, StopInstanceResult>(command.InstanceGuid, new InstanceActor.StopInstanceCommand(command.AuditLogUserGuid, command.StopStrategy));
	}

	private Task<InstanceActionResult<SendCommandToInstanceResult>> SendMinecraftCommand(SendCommandToInstanceCommand command) {
		return RequestInstance<InstanceActor.SendCommandToInstanceCommand, SendCommandToInstanceResult>(command.InstanceGuid, new InstanceActor.SendCommandToInstanceCommand(command.AuditLogUserGuid, command.Command));
	}

	private void ReceiveInstanceData(ReceiveInstanceDataCommand command) {
		UpdateInstanceData(command.Instance);
	}
}
