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
using Phantom.Controller.Database.Entities;
using Phantom.Controller.Minecraft;
using Phantom.Controller.Services.Instances;
using Phantom.Utils.Actor;
using Phantom.Utils.Actor.Mailbox;
using Phantom.Utils.Actor.Tasks;
using Phantom.Utils.Collections;
using Phantom.Utils.Logging;
using Phantom.Utils.Rpc;
using Phantom.Utils.Rpc.Runtime.Server;
using Serilog;

namespace Phantom.Controller.Services.Agents;

sealed class AgentActor : ReceiveActor<AgentActor.ICommand>, IWithTimers {
	private static readonly ILogger Logger = PhantomLogger.Create<AgentActor>();
	
	private static readonly TimeSpan DisconnectionRecheckInterval = TimeSpan.FromSeconds(5);
	private static readonly TimeSpan DisconnectionThreshold = TimeSpan.FromSeconds(12);
	
	public readonly record struct Init(
		Guid? LoggedInUserGuid,
		Guid AgentGuid,
		AgentConfiguration AgentConfiguration,
		AuthSecret AuthSecret,
		AgentRuntimeInfo AgentRuntimeInfo,
		AgentConnectionKeys AgentConnectionKeys,
		ControllerState ControllerState,
		MinecraftVersions MinecraftVersions,
		IDbContextProvider DbProvider,
		CancellationToken CancellationToken
	);
	
	public static Props<ICommand> Factory(Init init) {
		return Props<ICommand>.Create(() => new AgentActor(init), new ActorConfiguration { SupervisorStrategy = SupervisorStrategies.Resume, MailboxType = UnboundedJumpAheadMailbox.Name });
	}
	
	public ITimerScheduler Timers { get; set; } = null!;
	
	private readonly AgentConnectionKeys agentConnectionKeys;
	private readonly ControllerState controllerState;
	private readonly MinecraftVersions minecraftVersions;
	private readonly IDbContextProvider dbProvider;
	private readonly CancellationToken cancellationToken;
	
	private readonly Guid agentGuid;
	private readonly AuthInfo authInfo;
	
	private AgentConfiguration configuration;
	private AgentRuntimeInfo runtimeInfo;
	private AgentStats? stats;
	private ImmutableArray<TaggedJavaRuntime> javaRuntimes = ImmutableArray<TaggedJavaRuntime>.Empty;
	
	private string AgentName => configuration.AgentName;
	
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
		this.agentConnectionKeys = init.AgentConnectionKeys;
		this.controllerState = init.ControllerState;
		this.minecraftVersions = init.MinecraftVersions;
		this.dbProvider = init.DbProvider;
		this.cancellationToken = init.CancellationToken;
		
		this.agentGuid = init.AgentGuid;
		this.authInfo = new AuthInfo(this, init.AuthSecret);
		
		this.configuration = init.AgentConfiguration;
		this.runtimeInfo = init.AgentRuntimeInfo;
		this.connection = new AgentConnection(agentGuid, configuration.AgentName);
		
		this.databaseStorageActor = Context.ActorOf(AgentDatabaseStorageActor.Factory(new AgentDatabaseStorageActor.Init(agentGuid, init.DbProvider, init.CancellationToken)), "DatabaseStorage");
		
		if (init.LoggedInUserGuid is {} loggedInUserGuid) {
			databaseStorageActor.Tell(new AgentDatabaseStorageActor.StoreAgentConfigurationCommand(loggedInUserGuid, configuration));
		}
		
		NotifyAgentUpdated();
		
		ReceiveAsync<InitializeCommand>(Initialize);
		Receive<ConfigureAgentCommand>(ConfigureAgent);
		ReceiveAsyncAndReply<RegisterCommand, ImmutableArray<ConfigureInstanceMessage>>(Register);
		Receive<SetConnectionCommand>(SetConnection);
		Receive<UnregisterCommand>(Unregister);
		ReceiveAndReply<GetAuthSecretCommand, AuthSecret>(GetAuthSecret);
		Receive<RefreshConnectionStatusCommand>(RefreshConnectionStatus);
		Receive<NotifyIsAliveCommand>(NotifyIsAlive);
		Receive<UpdateStatsCommand>(UpdateStats);
		ReceiveAndReplyLater<CreateOrUpdateInstanceCommand, Result<CreateOrUpdateInstanceResult, InstanceActionFailure>>(CreateOrUpdateInstance);
		Receive<UpdateInstanceStatusCommand>(UpdateInstanceStatus);
		Receive<UpdateInstancePlayerCountsCommand>(UpdateInstancePlayerCounts);
		ReceiveAndReplyLater<LaunchInstanceCommand, Result<LaunchInstanceResult, InstanceActionFailure>>(LaunchInstance);
		ReceiveAndReplyLater<StopInstanceCommand, Result<StopInstanceResult, InstanceActionFailure>>(StopInstance);
		ReceiveAndReplyLater<SendCommandToInstanceCommand, Result<SendCommandToInstanceResult, InstanceActionFailure>>(SendMinecraftCommand);
		Receive<ReceiveInstanceDataCommand>(ReceiveInstanceData);
	}
	
	private void NotifyAgentUpdated() {
		controllerState.UpdateAgent(new Agent(agentGuid, configuration, authInfo.ConnectionKey, runtimeInfo, stats, ConnectionStatus));
	}
	
	protected override void PreStart() {
		Self.Tell(new InitializeCommand());
		
		Timers.StartPeriodicTimer("RefreshConnectionStatus", new RefreshConnectionStatusCommand(), DisconnectionRecheckInterval, Self);
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
	
	private async Task<Result<TReply, InstanceActionFailure>> RequestInstance<TCommand, TReply>(Guid instanceGuid, TCommand command) where TCommand : InstanceActor.ICommand, ICanReply<Result<TReply, InstanceActionFailure>> {
		if (instanceActorByGuid.TryGetValue(instanceGuid, out var instance)) {
			return await instance.Request(command, cancellationToken);
		}
		else {
			Logger.Warning("Could not deliver command {CommandType} to instance {InstanceGuid}, instance not found.", command.GetType().Name, instanceGuid);
			return InstanceActionFailure.InstanceDoesNotExist;
		}
	}
	
	private async Task<ImmutableArray<ConfigureInstanceMessage>> PrepareInitialConfigurationMessages() {
		var configurationMessages = ImmutableArray.CreateBuilder<ConfigureInstanceMessage>();
		
		foreach (var (instanceGuid, instanceConfiguration, _, _, launchAutomatically) in instanceDataByGuid.Values.ToImmutableArray()) {
			var serverExecutableInfo = await minecraftVersions.GetServerExecutableInfo(instanceConfiguration.MinecraftVersion, cancellationToken);
			configurationMessages.Add(new ConfigureInstanceMessage(instanceGuid, instanceConfiguration, new InstanceLaunchProperties(serverExecutableInfo), launchAutomatically));
		}
		
		return configurationMessages.ToImmutable();
	}
	
	public interface ICommand;
	
	private sealed record InitializeCommand : ICommand;
	
	public sealed record ConfigureAgentCommand(Guid LoggedInUserGuid, AgentConfiguration Configuration) : ICommand;
	
	public sealed record RegisterCommand(AgentRuntimeInfo RuntimeInfo, ImmutableArray<TaggedJavaRuntime> JavaRuntimes) : ICommand, ICanReply<ImmutableArray<ConfigureInstanceMessage>>;
	
	public sealed record SetConnectionCommand(RpcServerToClientConnection<IMessageToController, IMessageToAgent> Connection) : ICommand;
	
	public sealed record UnregisterCommand : ICommand;
	
	public sealed record GetAuthSecretCommand : ICommand, ICanReply<AuthSecret>;
	
	private sealed record RefreshConnectionStatusCommand : ICommand;
	
	public sealed record NotifyIsAliveCommand : ICommand;
	
	public sealed record UpdateStatsCommand(int RunningInstanceCount, RamAllocationUnits RunningInstanceMemory) : ICommand;
	
	public sealed record CreateOrUpdateInstanceCommand(Guid LoggedInUserGuid, Guid InstanceGuid, InstanceConfiguration Configuration) : ICommand, ICanReply<Result<CreateOrUpdateInstanceResult, InstanceActionFailure>>;
	
	public sealed record UpdateInstanceStatusCommand(Guid InstanceGuid, IInstanceStatus Status) : ICommand;
	
	public sealed record UpdateInstancePlayerCountsCommand(Guid InstanceGuid, InstancePlayerCounts? PlayerCounts) : ICommand;
	
	public sealed record LaunchInstanceCommand(Guid LoggedInUserGuid, Guid InstanceGuid) : ICommand, ICanReply<Result<LaunchInstanceResult, InstanceActionFailure>>;
	
	public sealed record StopInstanceCommand(Guid LoggedInUserGuid, Guid InstanceGuid, MinecraftStopStrategy StopStrategy) : ICommand, ICanReply<Result<StopInstanceResult, InstanceActionFailure>>;
	
	public sealed record SendCommandToInstanceCommand(Guid LoggedInUserGuid, Guid InstanceGuid, string Command) : ICommand, ICanReply<Result<SendCommandToInstanceResult, InstanceActionFailure>>;
	
	public sealed record ReceiveInstanceDataCommand(Instance Instance) : ICommand, IJumpAhead;
	
	private async Task Initialize(InitializeCommand command) {
		ImmutableArray<InstanceEntity> instanceEntities;
		await using (var ctx = dbProvider.Eager()) {
			instanceEntities = await ctx.Instances.Where(instance => instance.AgentGuid == agentGuid).AsAsyncEnumerable().ToImmutableArrayCatchingExceptionsAsync(OnException, cancellationToken);
		}
		
		static void OnException(Exception e) {
			Logger.Error(e, "Could not load instance from database.");
		}
		
		foreach (var instanceEntity in instanceEntities) {
			var instanceConfiguration = new InstanceConfiguration(
				instanceEntity.AgentGuid,
				instanceEntity.InstanceName,
				instanceEntity.ServerPort,
				instanceEntity.RconPort,
				instanceEntity.MinecraftVersion,
				instanceEntity.MinecraftServerKind,
				instanceEntity.MemoryAllocation,
				instanceEntity.JavaRuntimeGuid,
				JvmArgumentsHelper.Split(instanceEntity.JvmArguments)
			);
			
			CreateNewInstance(Instance.Offline(instanceEntity.InstanceGuid, instanceConfiguration, instanceEntity.LaunchAutomatically));
		}
	}
	
	private void ConfigureAgent(ConfigureAgentCommand message) {
		configuration = message.Configuration;
		NotifyAgentUpdated();
		
		databaseStorageActor.Tell(new AgentDatabaseStorageActor.StoreAgentConfigurationCommand(message.LoggedInUserGuid, configuration));
	}
	
	private async Task<ImmutableArray<ConfigureInstanceMessage>> Register(RegisterCommand command) {
		var configurationMessages = await PrepareInitialConfigurationMessages();
		
		runtimeInfo = command.RuntimeInfo;
		lastPingTime = DateTimeOffset.Now;
		isOnline = true;
		NotifyAgentUpdated();
		
		Logger.Information("Registered agent \"{AgentName}\" (GUID {AgentGuid}).", AgentName, agentGuid);
		
		databaseStorageActor.Tell(new AgentDatabaseStorageActor.StoreAgentRuntimeInfoCommand(runtimeInfo));
		
		javaRuntimes = command.JavaRuntimes;
		controllerState.UpdateAgentJavaRuntimes(agentGuid, javaRuntimes);
		
		return configurationMessages;
	}
	
	private void SetConnection(SetConnectionCommand command) {
		connection.UpdateConnection(command.Connection);
	}
	
	private void Unregister(UnregisterCommand command) {
		connection.Close();
		
		stats = null;
		lastPingTime = null;
		isOnline = false;
		NotifyAgentUpdated();
		
		TellAllInstances(new InstanceActor.SetStatusCommand(InstanceStatus.Offline));
		
		Logger.Information("Unregistered agent \"{AgentName}\" (GUID {AgentGuid}).", AgentName, agentGuid);
	}
	
	private AuthSecret GetAuthSecret(GetAuthSecretCommand command) {
		return authInfo.Secret;
	}
	
	private void RefreshConnectionStatus(RefreshConnectionStatusCommand command) {
		if (isOnline && lastPingTime != null && DateTimeOffset.Now - lastPingTime >= DisconnectionThreshold) {
			isOnline = false;
			NotifyAgentUpdated();
			
			Logger.Warning("Lost connection to agent \"{AgentName}\" (GUID {AgentGuid}).", AgentName, agentGuid);
		}
	}
	
	private void NotifyIsAlive(NotifyIsAliveCommand command) {
		lastPingTime = DateTimeOffset.Now;
		
		if (!isOnline) {
			isOnline = true;
			NotifyAgentUpdated();
			
			Logger.Warning("Restored connection to agent \"{AgentName}\" (GUID {AgentGuid}).", AgentName, agentGuid);
		}
	}
	
	private void UpdateStats(UpdateStatsCommand command) {
		stats = new AgentStats(command.RunningInstanceCount, command.RunningInstanceMemory);
		NotifyAgentUpdated();
	}
	
	private Task<Result<CreateOrUpdateInstanceResult, InstanceActionFailure>> CreateOrUpdateInstance(CreateOrUpdateInstanceCommand command) {
		var instanceConfiguration = command.Configuration;
		
		if (string.IsNullOrWhiteSpace(instanceConfiguration.InstanceName)) {
			return Task.FromResult<Result<CreateOrUpdateInstanceResult, InstanceActionFailure>>(CreateOrUpdateInstanceResult.InstanceNameMustNotBeEmpty);
		}
		
		if (instanceConfiguration.MemoryAllocation <= RamAllocationUnits.Zero) {
			return Task.FromResult<Result<CreateOrUpdateInstanceResult, InstanceActionFailure>>(CreateOrUpdateInstanceResult.InstanceMemoryMustNotBeZero);
		}
		
		return minecraftVersions.GetServerExecutableInfo(instanceConfiguration.MinecraftVersion, cancellationToken)
		                        .ContinueOnActor(CreateOrUpdateInstance1, command)
		                        .Unwrap();
	}
	
	private Task<Result<CreateOrUpdateInstanceResult, InstanceActionFailure>> CreateOrUpdateInstance1(FileDownloadInfo? serverExecutableInfo, CreateOrUpdateInstanceCommand command) {
		if (serverExecutableInfo == null) {
			return Task.FromResult<Result<CreateOrUpdateInstanceResult, InstanceActionFailure>>(CreateOrUpdateInstanceResult.MinecraftVersionDownloadInfoNotFound);
		}
		
		var instanceConfiguration = command.Configuration;
		
		bool isCreatingInstance = !instanceActorByGuid.TryGetValue(command.InstanceGuid, out var instanceActorRef);
		if (isCreatingInstance) {
			instanceActorRef = CreateNewInstance(Instance.Offline(command.InstanceGuid, instanceConfiguration));
		}
		
		var configureInstanceCommand = new InstanceActor.ConfigureInstanceCommand(command.LoggedInUserGuid, command.InstanceGuid, instanceConfiguration, new InstanceLaunchProperties(serverExecutableInfo), isCreatingInstance);
		
		return instanceActorRef.Request(configureInstanceCommand, cancellationToken)
		                       .ContinueOnActor(CreateOrUpdateInstance2, configureInstanceCommand);
	}
	
	private Result<CreateOrUpdateInstanceResult, InstanceActionFailure> CreateOrUpdateInstance2(Result<ConfigureInstanceResult, InstanceActionFailure> result, InstanceActor.ConfigureInstanceCommand command) {
		var instanceGuid = command.InstanceGuid;
		var instanceName = command.Configuration.InstanceName;
		var isCreating = command.IsCreatingInstance;
		
		if (result.Is(ConfigureInstanceResult.Success)) {
			string action = isCreating ? "Created" : "Edited";
			Logger.Information(action + " instance \"{InstanceName}\" (GUID {InstanceGuid}) in agent \"{AgentName}\".", instanceName, instanceGuid, AgentName);
			
			return CreateOrUpdateInstanceResult.Success;
		}
		else {
			string action = isCreating ? "creating" : "editing";
			string reason = result.Into(ConfigureInstanceResultExtensions.ToSentence, InstanceActionFailureExtensions.ToSentence);
			Logger.Information("Failed " + action + " instance \"{InstanceName}\" (GUID {InstanceGuid}) in agent \"{AgentName}\". {ErrorMessage}", instanceName, instanceGuid, AgentName, reason);
			
			return CreateOrUpdateInstanceResult.UnknownError;
		}
	}
	
	private void UpdateInstanceStatus(UpdateInstanceStatusCommand command) {
		TellInstance(command.InstanceGuid, new InstanceActor.SetStatusCommand(command.Status));
	}
	
	private void UpdateInstancePlayerCounts(UpdateInstancePlayerCountsCommand command) {
		TellInstance(command.InstanceGuid, new InstanceActor.SetPlayerCountsCommand(command.PlayerCounts));
	}
	
	private Task<Result<LaunchInstanceResult, InstanceActionFailure>> LaunchInstance(LaunchInstanceCommand command) {
		return RequestInstance<InstanceActor.LaunchInstanceCommand, LaunchInstanceResult>(command.InstanceGuid, new InstanceActor.LaunchInstanceCommand(command.LoggedInUserGuid));
	}
	
	private Task<Result<StopInstanceResult, InstanceActionFailure>> StopInstance(StopInstanceCommand command) {
		return RequestInstance<InstanceActor.StopInstanceCommand, StopInstanceResult>(command.InstanceGuid, new InstanceActor.StopInstanceCommand(command.LoggedInUserGuid, command.StopStrategy));
	}
	
	private Task<Result<SendCommandToInstanceResult, InstanceActionFailure>> SendMinecraftCommand(SendCommandToInstanceCommand command) {
		return RequestInstance<InstanceActor.SendCommandToInstanceCommand, SendCommandToInstanceResult>(command.InstanceGuid, new InstanceActor.SendCommandToInstanceCommand(command.LoggedInUserGuid, command.Command));
	}
	
	private void ReceiveInstanceData(ReceiveInstanceDataCommand command) {
		UpdateInstanceData(command.Instance);
	}
	
	private sealed class AuthInfo {
		private readonly AgentActor actor;
		
		public AuthSecret Secret { get; private set; }
		public ImmutableArray<byte> ConnectionKey { get; private set; }
		
		public AuthInfo(AgentActor actor, AuthSecret authSecret) {
			this.actor = actor;
			this.Secret = authSecret;
			this.ConnectionKey = CreateConnectionKey(authSecret);
		}
		
		public void UpdateSecret(AuthSecret newSecret) {
			this.Secret = newSecret;
			this.ConnectionKey = CreateConnectionKey(newSecret);
		}
		
		private ImmutableArray<byte> CreateConnectionKey(AuthSecret authSecret) {
			return actor.agentConnectionKeys.Get(new AuthToken(actor.agentGuid, authSecret)).ToBytes();
		}
	}
}
