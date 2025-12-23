using System.Collections.Concurrent;
using System.Collections.Immutable;
using Akka.Actor;
using Microsoft.EntityFrameworkCore;
using Phantom.Common.Data;
using Phantom.Common.Data.Replies;
using Phantom.Common.Data.Web.Agent;
using Phantom.Common.Data.Web.Users;
using Phantom.Common.Messages.Agent.Handshake;
using Phantom.Common.Messages.Agent.ToAgent;
using Phantom.Controller.Database;
using Phantom.Controller.Database.Entities;
using Phantom.Controller.Minecraft;
using Phantom.Controller.Services.Users.Sessions;
using Phantom.Utils.Actor;
using Phantom.Utils.Logging;
using Phantom.Utils.Rpc;
using Serilog;

namespace Phantom.Controller.Services.Agents;

sealed class AgentManager(
	IActorRefFactory actorSystem,
	AgentConnectionKeys agentConnectionKeys,
	ControllerState controllerState,
	MinecraftVersions minecraftVersions,
	IDbContextProvider dbProvider,
	CancellationToken cancellationToken
) {
	private static readonly ILogger Logger = PhantomLogger.Create<AgentManager>();
	
	private readonly ConcurrentDictionary<Guid, ActorRef<AgentActor.ICommand>> agentsByAgentGuid = new ();
	
	public async Task Initialize() {
		await using var ctx = dbProvider.Eager();
		await Migrate(ctx);
		
		await foreach (var entity in ctx.Agents.AsAsyncEnumerable().WithCancellation(cancellationToken)) {
			var agentGuid = entity.AgentGuid;
			
			if (AddAgent(loggedInUserGuid: null, agentGuid, entity.Configuration, entity.AuthSecret!, entity.RuntimeInfo)) {
				Logger.Information("Loaded agent \"{AgentName}\" (GUID {AgentGuid}) from database.", entity.Name, agentGuid);
			}
		}
	}
	
	private bool AddAgent(Guid? loggedInUserGuid, Guid agentGuid, AgentConfiguration configuration, AuthSecret authSecret, AgentRuntimeInfo runtimeInfo) {
		var init = new AgentActor.Init(loggedInUserGuid, agentGuid, configuration, authSecret, runtimeInfo, agentConnectionKeys, controllerState, minecraftVersions, dbProvider, cancellationToken);
		var name = "Agent:" + agentGuid;
		return agentsByAgentGuid.TryAdd(agentGuid, actorSystem.ActorOf(AgentActor.Factory(init), name));
	}
	
	private async Task Migrate(ApplicationDbContext ctx) {
		List<AgentEntity> agentsWithoutSecrets = await ctx.Agents.Where(static entity => entity.AuthSecret == null).ToListAsync(cancellationToken);
		if (agentsWithoutSecrets.Count == 0) {
			return;
		}
		
		foreach (var entity in agentsWithoutSecrets) {
			entity.AuthSecret = AuthSecret.Generate();
		}
		
		await ctx.SaveChangesAsync(cancellationToken);
	}
	
	public async Task<ImmutableArray<ConfigureInstanceMessage>?> RegisterAgent(Guid agentGuid, AgentRegistration registration) {
		if (!agentsByAgentGuid.TryGetValue(agentGuid, out var agentActor)) {
			return null;
		}
		
		var runtimeInfo = AgentRuntimeInfo.From(registration.AgentInfo);
		return await agentActor.Request(new AgentActor.RegisterCommand(runtimeInfo, registration.JavaRuntimes), cancellationToken);
	}
	
	public async Task<AuthSecret?> GetAgentAuthSecret(Guid agentGuid) {
		if (agentsByAgentGuid.TryGetValue(agentGuid, out var agent)) {
			return await agent.Request(new AgentActor.GetAuthSecretCommand(), cancellationToken);
		}
		else {
			return null;
		}
	}
	
	public bool TellAgent(Guid agentGuid, AgentActor.ICommand command) {
		if (agentsByAgentGuid.TryGetValue(agentGuid, out var agent)) {
			agent.Tell(command);
			return true;
		}
		else {
			Logger.Warning("Could not deliver command {CommandType} to unknown agent {AgentGuid}.", command.GetType().Name, agentGuid);
			return false;
		}
	}
	
	public Result<CreateOrUpdateAgentResult, UserActionFailure> CreateOrUpdateAgent(LoggedInUser loggedInUser, Guid agentGuid, AgentConfiguration configuration) {
		if (!loggedInUser.CheckPermission(Permission.ManageAllAgents)) {
			return UserActionFailure.NotAuthorized;
		}
		
		if (configuration.AgentName.Length == 0) {
			return CreateOrUpdateAgentResult.AgentNameMustNotBeEmpty;
		}
		
		if (agentsByAgentGuid.TryGetValue(agentGuid, out var agent)) {
			agent.Tell(new AgentActor.ConfigureAgentCommand(loggedInUser.Guid!.Value, configuration));
		}
		else {
			AddAgent(loggedInUser.Guid!.Value, agentGuid, configuration, AuthSecret.Generate(), new AgentRuntimeInfo());
		}
		
		return CreateOrUpdateAgentResult.Success;
	}
	
	public async Task<Result<TReply, UserInstanceActionFailure>> DoInstanceAction<TCommand, TReply>(LoggedInUser loggedInUser, Permission requiredPermission, Guid agentGuid, Func<Guid, TCommand> commandFactoryFromLoggedInUserGuid) where TCommand : class, AgentActor.ICommand, ICanReply<Result<TReply, InstanceActionFailure>> {
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
