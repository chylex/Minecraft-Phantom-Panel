using System.Collections.Immutable;
using Akka.Actor;
using Phantom.Common.Data;
using Phantom.Common.Data.Java;
using Phantom.Common.Data.Minecraft;
using Phantom.Common.Data.Replies;
using Phantom.Common.Data.Web.AuditLog;
using Phantom.Common.Data.Web.EventLog;
using Phantom.Common.Data.Web.Instance;
using Phantom.Common.Data.Web.Users;
using Phantom.Common.Messages.Web;
using Phantom.Common.Messages.Web.BiDirectional;
using Phantom.Common.Messages.Web.ToController;
using Phantom.Common.Messages.Web.ToWeb;
using Phantom.Controller.Minecraft;
using Phantom.Controller.Services.Agents;
using Phantom.Controller.Services.Events;
using Phantom.Controller.Services.Instances;
using Phantom.Controller.Services.Users;
using Phantom.Utils.Actor;
using Phantom.Utils.Logging;
using Phantom.Utils.Rpc.Message;
using Phantom.Utils.Rpc.Runtime;
using Serilog;
using Agent = Phantom.Common.Data.Web.Agent.Agent;

namespace Phantom.Controller.Services.Rpc;

public sealed class WebMessageListener : IMessageToControllerListener {
	private static readonly ILogger Logger = PhantomLogger.Create<WebMessageListener>();

	private static int listenerSequenceId = 0;

	private readonly ActorRef<ICommand> actor;
	private readonly RpcConnectionToClient<IMessageToWebListener> connection;
	private readonly AuthToken authToken;
	private readonly ControllerState controllerState;
	private readonly UserManager userManager;
	private readonly RoleManager roleManager;
	private readonly UserRoleManager userRoleManager;
	private readonly UserLoginManager userLoginManager;
	private readonly AuditLogManager auditLogManager;
	private readonly AgentManager agentManager;
	private readonly InstanceLogManager instanceLogManager;
	private readonly MinecraftVersions minecraftVersions;
	private readonly EventLogManager eventLogManager;

	internal WebMessageListener(
		IActorRefFactory actorSystem,
		RpcConnectionToClient<IMessageToWebListener> connection,
		AuthToken authToken,
		ControllerState controllerState,
		UserManager userManager,
		RoleManager roleManager,
		UserRoleManager userRoleManager,
		UserLoginManager userLoginManager,
		AuditLogManager auditLogManager,
		AgentManager agentManager,
		InstanceLogManager instanceLogManager,
		MinecraftVersions minecraftVersions,
		EventLogManager eventLogManager
	) {
		this.actor = actorSystem.ActorOf(Actor.Factory(this), "Web-" + Interlocked.Increment(ref listenerSequenceId));
		this.connection = connection;
		this.authToken = authToken;
		this.controllerState = controllerState;
		this.userManager = userManager;
		this.roleManager = roleManager;
		this.userRoleManager = userRoleManager;
		this.userLoginManager = userLoginManager;
		this.auditLogManager = auditLogManager;
		this.agentManager = agentManager;
		this.instanceLogManager = instanceLogManager;
		this.minecraftVersions = minecraftVersions;
		this.eventLogManager = eventLogManager;
	}

	private sealed class Actor : ReceiveActor<ICommand> {
		public static Props<ICommand> Factory(WebMessageListener listener) {
			return Props<ICommand>.Create(() => new Actor(listener), new ActorConfiguration { SupervisorStrategy = SupervisorStrategies.Resume });
		}

		private readonly WebMessageListener listener;

		private Actor(WebMessageListener listener) {
			this.listener = listener;

			Receive<StartConnectionCommand>(StartConnection);
			Receive<StopConnectionCommand>(StopConnection);
			Receive<RefreshAgentsCommand>(RefreshAgents);
			Receive<RefreshInstancesCommand>(RefreshInstances);
		}

		private void StartConnection(StartConnectionCommand command) {
			listener.controllerState.AgentsByGuidReceiver.Register(SelfTyped, static state => new RefreshAgentsCommand(state));
			listener.controllerState.InstancesByGuidReceiver.Register(SelfTyped, static state => new RefreshInstancesCommand(state));

			listener.instanceLogManager.LogsReceived += HandleInstanceLogsReceived;
		}

		private void StopConnection(StopConnectionCommand command) {
			listener.instanceLogManager.LogsReceived -= HandleInstanceLogsReceived;

			listener.controllerState.AgentsByGuidReceiver.Unregister(SelfTyped);
			listener.controllerState.InstancesByGuidReceiver.Unregister(SelfTyped);
		}

		private void RefreshAgents(RefreshAgentsCommand command) {
			var message = new RefreshAgentsMessage(command.Agents.Values.ToImmutableArray());
			listener.connection.Send(message);
		}

		private void RefreshInstances(RefreshInstancesCommand command) {
			var message = new RefreshInstancesMessage(command.Instances.Values.ToImmutableArray());
			listener.connection.Send(message);
		}

		private void HandleInstanceLogsReceived(object? sender, InstanceLogManager.Event e) {
			listener.connection.Send(new InstanceOutputMessage(e.InstanceGuid, e.Lines));
		}
	}

	private interface ICommand {}

	private sealed record StartConnectionCommand : ICommand;

	private sealed record StopConnectionCommand : ICommand;

	private sealed record RefreshAgentsCommand(ImmutableDictionary<Guid, Agent> Agents) : ICommand;

	private sealed record RefreshInstancesCommand(ImmutableDictionary<Guid, Instance> Instances) : ICommand;

	public async Task<NoReply> HandleRegisterWeb(RegisterWebMessage message) {
		if (authToken.FixedTimeEquals(message.AuthToken)) {
			Logger.Information("Web authorized successfully.");
			connection.SetAuthorizationResult(true);
			await connection.Send(new RegisterWebResultMessage(true));
		}
		else {
			Logger.Warning("Web failed to authorize, invalid token.");
			connection.SetAuthorizationResult(false);
			await connection.Send(new RegisterWebResultMessage(false));
		}

		if (!connection.IsClosed) {
			actor.Tell(new StartConnectionCommand());
		}

		return NoReply.Instance;
	}

	public Task<NoReply> HandleUnregisterWeb(UnregisterWebMessage message) {
		if (!connection.IsClosed) {
			connection.Close();
			actor.Tell(new StopConnectionCommand());
		}

		return Task.FromResult(NoReply.Instance);
	}

	public Task<CreateOrUpdateAdministratorUserResult> HandleCreateOrUpdateAdministratorUser(CreateOrUpdateAdministratorUserMessage message) {
		return userManager.CreateOrUpdateAdministrator(message.Username, message.Password);
	}

	public Task<CreateUserResult> HandleCreateUser(CreateUserMessage message) {
		return userManager.Create(message.LoggedInUserGuid, message.Username, message.Password);
	}

	public Task<ImmutableArray<UserInfo>> HandleGetUsers(GetUsersMessage message) {
		return userManager.GetAll();
	}

	public Task<ImmutableArray<RoleInfo>> HandleGetRoles(GetRolesMessage message) {
		return roleManager.GetAll();
	}

	public Task<ImmutableDictionary<Guid, ImmutableArray<Guid>>> HandleGetUserRoles(GetUserRolesMessage message) {
		return userRoleManager.GetUserRoles(message.UserGuids);
	}

	public Task<ChangeUserRolesResult> HandleChangeUserRoles(ChangeUserRolesMessage message) {
		return userRoleManager.ChangeUserRoles(message.LoggedInUserGuid, message.SubjectUserGuid, message.AddToRoleGuids, message.RemoveFromRoleGuids);
	}

	public Task<DeleteUserResult> HandleDeleteUser(DeleteUserMessage message) {
		return userManager.DeleteByGuid(message.LoggedInUserGuid, message.SubjectUserGuid);
	}

	public Task<InstanceActionResult<CreateOrUpdateInstanceResult>> HandleCreateOrUpdateInstance(CreateOrUpdateInstanceMessage message) {
		return agentManager.DoInstanceAction<AgentActor.CreateOrUpdateInstanceCommand, CreateOrUpdateInstanceResult>(message.Configuration.AgentGuid, new AgentActor.CreateOrUpdateInstanceCommand(message.LoggedInUserGuid, message.InstanceGuid, message.Configuration));
	}

	public Task<InstanceActionResult<LaunchInstanceResult>> HandleLaunchInstance(LaunchInstanceMessage message) {
		return agentManager.DoInstanceAction<AgentActor.LaunchInstanceCommand, LaunchInstanceResult>(message.AgentGuid, new AgentActor.LaunchInstanceCommand(message.InstanceGuid, message.LoggedInUserGuid));
	}

	public Task<InstanceActionResult<StopInstanceResult>> HandleStopInstance(StopInstanceMessage message) {
		return agentManager.DoInstanceAction<AgentActor.StopInstanceCommand, StopInstanceResult>(message.AgentGuid, new AgentActor.StopInstanceCommand(message.InstanceGuid, message.LoggedInUserGuid, message.StopStrategy));
	}

	public Task<InstanceActionResult<SendCommandToInstanceResult>> HandleSendCommandToInstance(SendCommandToInstanceMessage message) {
		return agentManager.DoInstanceAction<AgentActor.SendCommandToInstanceCommand, SendCommandToInstanceResult>(message.AgentGuid, new AgentActor.SendCommandToInstanceCommand(message.InstanceGuid, message.LoggedInUserGuid, message.Command));
	}

	public Task<ImmutableArray<MinecraftVersion>> HandleGetMinecraftVersions(GetMinecraftVersionsMessage message) {
		return minecraftVersions.GetVersions(CancellationToken.None);
	}

	public Task<ImmutableDictionary<Guid, ImmutableArray<TaggedJavaRuntime>>> HandleGetAgentJavaRuntimes(GetAgentJavaRuntimesMessage message) {
		return Task.FromResult(controllerState.AgentJavaRuntimesByGuid);
	}

	public Task<ImmutableArray<AuditLogItem>> HandleGetAuditLog(GetAuditLogMessage message) {
		return auditLogManager.GetMostRecentItems(message.Count);
	}

	public Task<ImmutableArray<EventLogItem>> HandleGetEventLog(GetEventLogMessage message) {
		return eventLogManager.GetMostRecentItems(message.Count);
	}

	public Task<LogInSuccess?> HandleLogIn(LogInMessage message) {
		return userLoginManager.LogIn(message.Username, message.Password);
	}

	public Task<NoReply> HandleReply(ReplyMessage message) {
		connection.Receive(message);
		return Task.FromResult(NoReply.Instance);
	}
}
