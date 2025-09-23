using System.Collections.Concurrent;
using System.Collections.Immutable;
using Akka.Actor;
using Phantom.Common.Data;
using Phantom.Common.Data.Replies;
using Phantom.Common.Data.Web.Agent;
using Phantom.Common.Data.Web.Users;
using Phantom.Common.Messages.Agent.Handshake;
using Phantom.Common.Messages.Agent.ToAgent;
using Phantom.Controller.Database;
using Phantom.Controller.Minecraft;
using Phantom.Controller.Services.Users.Sessions;
using Phantom.Utils.Actor;
using Phantom.Utils.Logging;
using Serilog;

namespace Phantom.Controller.Services.Agents;

sealed class AgentManager {
	private static readonly ILogger Logger = PhantomLogger.Create<AgentManager>();
	
	private readonly IActorRefFactory actorSystem;
	private readonly ControllerState controllerState;
	private readonly MinecraftVersions minecraftVersions;
	private readonly UserLoginManager userLoginManager;
	private readonly IDbContextProvider dbProvider;
	private readonly CancellationToken cancellationToken;
	
	private readonly ConcurrentDictionary<Guid, ActorRef<AgentActor.ICommand>> agentsByAgentGuid = new ();
	private readonly Func<Guid, AgentConfiguration, ActorRef<AgentActor.ICommand>> addAgentActorFactory;
	
	public AgentManager(IActorRefFactory actorSystem, ControllerState controllerState, MinecraftVersions minecraftVersions, UserLoginManager userLoginManager, IDbContextProvider dbProvider, CancellationToken cancellationToken) {
		this.actorSystem = actorSystem;
		this.controllerState = controllerState;
		this.minecraftVersions = minecraftVersions;
		this.userLoginManager = userLoginManager;
		this.dbProvider = dbProvider;
		this.cancellationToken = cancellationToken;
		
		this.addAgentActorFactory = CreateAgentActor;
	}
	
	private ActorRef<AgentActor.ICommand> CreateAgentActor(Guid agentGuid, AgentConfiguration agentConfiguration) {
		var init = new AgentActor.Init(agentGuid, agentConfiguration, controllerState, minecraftVersions, dbProvider, cancellationToken);
		var name = "Agent:" + agentGuid;
		return actorSystem.ActorOf(AgentActor.Factory(init), name);
	}
	
	public async Task Initialize() {
		await using var ctx = dbProvider.Eager();
		
		await foreach (var entity in ctx.Agents.AsAsyncEnumerable().WithCancellation(cancellationToken)) {
			var agentGuid = entity.AgentGuid;
			var agentConfiguration = new AgentConfiguration(entity.Name, entity.ProtocolVersion, entity.BuildVersion, entity.MaxInstances, entity.MaxMemory);
			
			if (agentsByAgentGuid.TryAdd(agentGuid, CreateAgentActor(agentGuid, agentConfiguration))) {
				Logger.Information("Loaded agent \"{AgentName}\" (GUID {AgentGuid}) from database.", agentConfiguration.AgentName, agentGuid);
			}
		}
	}
	
	public async Task<ImmutableArray<ConfigureInstanceMessage>> RegisterAgent(AgentRegistration registration) {
		var agentConfiguration = AgentConfiguration.From(registration.AgentInfo);
		var agentActor = agentsByAgentGuid.GetOrAdd(registration.AgentInfo.AgentGuid, addAgentActorFactory, agentConfiguration);
		return await agentActor.Request(new AgentActor.RegisterCommand(agentConfiguration, registration.JavaRuntimes), cancellationToken);
	}
	
	public bool TellAgent(Guid agentGuid, AgentActor.ICommand command) {
		if (agentsByAgentGuid.TryGetValue(agentGuid, out var agent)) {
			agent.Tell(command);
			return true;
		}
		else {
			Logger.Warning("Could not deliver command {CommandType} to agent {AgentGuid}, agent not registered.", command.GetType().Name, agentGuid);
			return false;
		}
	}
	
	public async Task<Result<TReply, UserInstanceActionFailure>> DoInstanceAction<TCommand, TReply>(Permission requiredPermission, ImmutableArray<byte> authToken, Guid agentGuid, Func<Guid, TCommand> commandFactoryFromLoggedInUserGuid) where TCommand : class, AgentActor.ICommand, ICanReply<Result<TReply, InstanceActionFailure>> {
		var loggedInUser = userLoginManager.GetLoggedInUser(authToken);
		if (!loggedInUser.HasAccessToAgent(agentGuid) || !loggedInUser.CheckPermission(requiredPermission)) {
			return (UserInstanceActionFailure) UserActionFailure.NotAuthorized;
		}
		
		if (!agentsByAgentGuid.TryGetValue(agentGuid, out var agent)) {
			return (UserInstanceActionFailure) InstanceActionFailure.AgentDoesNotExist;
		}
		
		var command = commandFactoryFromLoggedInUserGuid(loggedInUser.Guid!.Value);
		var result = await agent.Request(command, cancellationToken);
		return result.MapError(static error => (UserInstanceActionFailure) error);
	}
}
