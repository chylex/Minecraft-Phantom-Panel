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
using Phantom.Common.Data.Web.Users;
using Phantom.Common.Messages.Agent;
using Phantom.Common.Messages.Agent.ToAgent;
using Phantom.Controller.Database;
using Phantom.Controller.Database.Entities;
using Phantom.Controller.Minecraft;
using Phantom.Controller.Services.Instances;
using Phantom.Controller.Services.Users.Sessions;
using Phantom.Utils.Actor;
using Phantom.Utils.Actor.Mailbox;
using Phantom.Utils.Actor.Tasks;
using Phantom.Utils.Collections;
using Phantom.Utils.Logging;
using Phantom.Utils.Rpc.Runtime;
using Serilog;

namespace Phantom.Controller.Services.Agents;

sealed class AgentActor : ReceiveActor<AgentActor.ICommand> {
	private static readonly ILogger Logger = PhantomLogger.Create<AgentActor>();

	private static readonly TimeSpan DisconnectionRecheckInterval = TimeSpan.FromSeconds(5);
	private static readonly TimeSpan DisconnectionThreshold = TimeSpan.FromSeconds(12);

	public readonly record struct Init(Guid AgentGuid, AgentConfiguration AgentConfiguration, ControllerState ControllerState, MinecraftVersions MinecraftVersions, UserLoginManager UserLoginManager, IDbContextProvider DbProvider, CancellationToken CancellationToken);
	
	public static Props<ICommand> Factory(Init init) {
		return Props<ICommand>.Create(() => new AgentActor(init), new ActorConfiguration { SupervisorStrategy = SupervisorStrategies.Resume, MailboxType = UnboundedJumpAheadMailbox.Name });
	}

	private readonly ControllerState controllerState;
	private readonly MinecraftVersions minecraftVersions;
	private readonly UserLoginManager userLoginManager;
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
		this.userLoginManager = init.UserLoginManager;
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
		ReceiveAndReplyLater<CreateOrUpdateInstanceCommand, Result<CreateOrUpdateInstanceResult, UserInstanceActionFailure>>(CreateOrUpdateInstance);
		Receive<UpdateInstanceStatusCommand>(UpdateInstanceStatus);
		ReceiveAndReplyLater<LaunchInstanceCommand, Result<LaunchInstanceResult, UserInstanceActionFailure>>(LaunchInstance);
		ReceiveAndReplyLater<StopInstanceCommand, Result<StopInstanceResult, UserInstanceActionFailure>>(StopInstance);
		ReceiveAndReplyLater<SendCommandToInstanceCommand, Result<SendCommandToInstanceResult, UserInstanceActionFailure>>(SendMinecraftCommand);
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

	private async Task<Result<TReply, UserInstanceActionFailure>> RequestInstance<TCommand, TReply>(ImmutableArray<byte> authToken, Guid instanceGuid, Func<Guid, TCommand> commandFactoryFromLoggedInUserGuid) where TCommand : InstanceActor.ICommand, ICanReply<Result<TReply, InstanceActionFailure>> {
		var loggedInUser = userLoginManager.GetLoggedInUser(authToken);
		if (!loggedInUser.CheckPermission(Permission.ControlInstances)) {
			return (UserInstanceActionFailure) UserActionFailure.NotAuthorized;
		}
		
		var command = commandFactoryFromLoggedInUserGuid(loggedInUser.Guid!.Value);
		
		if (instanceActorByGuid.TryGetValue(instanceGuid, out var instance)) {
			var result = await instance.Request(command, cancellationToken);
			return result.MapError(static error => (UserInstanceActionFailure) error);
		}
		else {
			Logger.Warning("Could not deliver command {CommandType} to instance {InstanceGuid}, instance not found.", command.GetType().Name, instanceGuid);
			return (UserInstanceActionFailure) InstanceActionFailure.InstanceDoesNotExist;
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
	
	public sealed record RegisterCommand(AgentConfiguration Configuration, RpcConnectionToClient<IMessageToAgent> Connection) : ICommand, ICanReply<ImmutableArray<ConfigureInstanceMessage>>;
	
	public sealed record UnregisterCommand(RpcConnectionToClient<IMessageToAgent> Connection) : ICommand;
	
	private sealed record RefreshConnectionStatusCommand : ICommand;
	
	public sealed record NotifyIsAliveCommand : ICommand;
	
	public sealed record UpdateStatsCommand(int RunningInstanceCount, RamAllocationUnits RunningInstanceMemory) : ICommand;
	
	public sealed record UpdateJavaRuntimesCommand(ImmutableArray<TaggedJavaRuntime> JavaRuntimes) : ICommand;
	
	public sealed record CreateOrUpdateInstanceCommand(ImmutableArray<byte> AuthToken, Guid InstanceGuid, InstanceConfiguration Configuration) : ICommand, ICanReply<Result<CreateOrUpdateInstanceResult, UserInstanceActionFailure>>;
	
	public sealed record UpdateInstanceStatusCommand(Guid InstanceGuid, IInstanceStatus Status) : ICommand;

	public sealed record LaunchInstanceCommand(ImmutableArray<byte> AuthToken, Guid InstanceGuid) : ICommand, ICanReply<Result<LaunchInstanceResult, UserInstanceActionFailure>>;
	
	public sealed record StopInstanceCommand(ImmutableArray<byte> AuthToken, Guid InstanceGuid, MinecraftStopStrategy StopStrategy) : ICommand, ICanReply<Result<StopInstanceResult, UserInstanceActionFailure>>;
	
	public sealed record SendCommandToInstanceCommand(ImmutableArray<byte> AuthToken, Guid InstanceGuid, string Command) : ICommand, ICanReply<Result<SendCommandToInstanceResult, UserInstanceActionFailure>>;
	
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
	
	private Task<Result<CreateOrUpdateInstanceResult, UserInstanceActionFailure>> CreateOrUpdateInstance(CreateOrUpdateInstanceCommand command) {
		var loggedInUser = userLoginManager.GetLoggedInUser(command.AuthToken);
		if (!loggedInUser.CheckPermission(Permission.CreateInstances)) {
			return Task.FromResult<Result<CreateOrUpdateInstanceResult, UserInstanceActionFailure>>((UserInstanceActionFailure) UserActionFailure.NotAuthorized);
		}
		
		var instanceConfiguration = command.Configuration;

		if (string.IsNullOrWhiteSpace(instanceConfiguration.InstanceName)) {
			return Task.FromResult<Result<CreateOrUpdateInstanceResult, UserInstanceActionFailure>>(CreateOrUpdateInstanceResult.InstanceNameMustNotBeEmpty);
		}
		
		if (instanceConfiguration.MemoryAllocation <= RamAllocationUnits.Zero) {
			return Task.FromResult<Result<CreateOrUpdateInstanceResult, UserInstanceActionFailure>>(CreateOrUpdateInstanceResult.InstanceMemoryMustNotBeZero);
		}
		
		return minecraftVersions.GetServerExecutableInfo(instanceConfiguration.MinecraftVersion, cancellationToken)
		                        .ContinueOnActor(CreateOrUpdateInstance1, loggedInUser.Guid!.Value, command)
		                        .Unwrap();
	}

	private Task<Result<CreateOrUpdateInstanceResult, UserInstanceActionFailure>> CreateOrUpdateInstance1(FileDownloadInfo? serverExecutableInfo, Guid loggedInUserGuid, CreateOrUpdateInstanceCommand command) {
		if (serverExecutableInfo == null) {
			return Task.FromResult<Result<CreateOrUpdateInstanceResult, UserInstanceActionFailure>>(CreateOrUpdateInstanceResult.MinecraftVersionDownloadInfoNotFound);
		}
		
		var instanceConfiguration = command.Configuration;
		
		bool isCreatingInstance = !instanceActorByGuid.TryGetValue(command.InstanceGuid, out var instanceActorRef);
		if (isCreatingInstance) {
			instanceActorRef = CreateNewInstance(Instance.Offline(command.InstanceGuid, instanceConfiguration));
		}
		
		var configureInstanceCommand = new InstanceActor.ConfigureInstanceCommand(loggedInUserGuid, command.InstanceGuid, instanceConfiguration, new InstanceLaunchProperties(serverExecutableInfo), isCreatingInstance);
		
		return instanceActorRef.Request(configureInstanceCommand, cancellationToken)
		                       .ContinueOnActor(CreateOrUpdateInstance2, configureInstanceCommand);
	}
	
	private Result<CreateOrUpdateInstanceResult, UserInstanceActionFailure> CreateOrUpdateInstance2(Result<ConfigureInstanceResult, InstanceActionFailure> result, InstanceActor.ConfigureInstanceCommand command) {
		var instanceGuid = command.InstanceGuid;
		var instanceName = command.Configuration.InstanceName;
		var isCreating = command.IsCreatingInstance;

		if (result.Is(ConfigureInstanceResult.Success)) {
			string action = isCreating ? "Added" : "Edited";
			string relation = isCreating ? "to agent" : "in agent";
			
			Logger.Information(action + " instance \"{InstanceName}\" (GUID {InstanceGuid}) " + relation + " \"{AgentName}\".", instanceName, instanceGuid, configuration.AgentName);
			
			return CreateOrUpdateInstanceResult.Success;
		}
		else {
			string action = isCreating ? "adding" : "editing";
			string relation = isCreating ? "to agent" : "in agent";
			string reason = result.Into(ConfigureInstanceResultExtensions.ToSentence, InstanceActionFailureExtensions.ToSentence);
			
			Logger.Information("Failed " + action + " instance \"{InstanceName}\" (GUID {InstanceGuid}) " + relation + " \"{AgentName}\". {ErrorMessage}", instanceName, instanceGuid, configuration.AgentName, reason);
			
			return CreateOrUpdateInstanceResult.UnknownError;
		}
	}
	
	private void UpdateInstanceStatus(UpdateInstanceStatusCommand command) {
		TellInstance(command.InstanceGuid, new InstanceActor.SetStatusCommand(command.Status));
	}

	private Task<Result<LaunchInstanceResult, UserInstanceActionFailure>> LaunchInstance(LaunchInstanceCommand command) {
		return RequestInstance<InstanceActor.LaunchInstanceCommand, LaunchInstanceResult>(command.AuthToken, command.InstanceGuid, static loggedInUserGuid => new InstanceActor.LaunchInstanceCommand(loggedInUserGuid));
	}

	private Task<Result<StopInstanceResult, UserInstanceActionFailure>> StopInstance(StopInstanceCommand command) {
		return RequestInstance<InstanceActor.StopInstanceCommand, StopInstanceResult>(command.AuthToken, command.InstanceGuid, loggedInUserGuid => new InstanceActor.StopInstanceCommand(loggedInUserGuid, command.StopStrategy));
	}

	private Task<Result<SendCommandToInstanceResult, UserInstanceActionFailure>> SendMinecraftCommand(SendCommandToInstanceCommand command) {
		return RequestInstance<InstanceActor.SendCommandToInstanceCommand, SendCommandToInstanceResult>(command.AuthToken, command.InstanceGuid, loggedInUserGuid => new InstanceActor.SendCommandToInstanceCommand(loggedInUserGuid, command.Command));
	}

	private void ReceiveInstanceData(ReceiveInstanceDataCommand command) {
		UpdateInstanceData(command.Instance);
	}
}
