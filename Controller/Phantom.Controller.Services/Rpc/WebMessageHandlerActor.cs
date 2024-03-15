using System.Collections.Immutable;
using Phantom.Common.Data.Java;
using Phantom.Common.Data.Minecraft;
using Phantom.Common.Data.Replies;
using Phantom.Common.Data.Web.AuditLog;
using Phantom.Common.Data.Web.EventLog;
using Phantom.Common.Data.Web.Instance;
using Phantom.Common.Data.Web.Users;
using Phantom.Common.Messages.Agent.BiDirectional;
using Phantom.Common.Messages.Web;
using Phantom.Common.Messages.Web.ToController;
using Phantom.Controller.Minecraft;
using Phantom.Controller.Services.Agents;
using Phantom.Controller.Services.Events;
using Phantom.Controller.Services.Instances;
using Phantom.Controller.Services.Users;
using Phantom.Utils.Actor;
using Phantom.Utils.Rpc.Runtime;

namespace Phantom.Controller.Services.Rpc;

sealed class WebMessageHandlerActor : ReceiveActor<IMessageToController> {
	public readonly record struct Init(
		RpcConnectionToClient<IMessageToWeb> Connection,
		WebRegistrationHandler WebRegistrationHandler,
		ControllerState ControllerState,
		InstanceLogManager InstanceLogManager,
		UserManager UserManager,
		RoleManager RoleManager,
		UserRoleManager UserRoleManager,
		UserLoginManager UserLoginManager,
		AuditLogManager AuditLogManager,
		AgentManager AgentManager,
		MinecraftVersions MinecraftVersions,
		EventLogManager EventLogManager
	);

	public static Props<IMessageToController> Factory(Init init) {
		return Props<IMessageToController>.Create(() => new WebMessageHandlerActor(init), new ActorConfiguration { SupervisorStrategy = SupervisorStrategies.Resume });
	}

	private readonly RpcConnectionToClient<IMessageToWeb> connection;
	private readonly WebRegistrationHandler webRegistrationHandler;
	private readonly ControllerState controllerState;
	private readonly UserManager userManager;
	private readonly RoleManager roleManager;
	private readonly UserRoleManager userRoleManager;
	private readonly UserLoginManager userLoginManager;
	private readonly AuditLogManager auditLogManager;
	private readonly AgentManager agentManager;
	private readonly MinecraftVersions minecraftVersions;
	private readonly EventLogManager eventLogManager;

	private WebMessageHandlerActor(Init init) {
		this.connection = init.Connection;
		this.webRegistrationHandler = init.WebRegistrationHandler;
		this.controllerState = init.ControllerState;
		this.userManager = init.UserManager;
		this.roleManager = init.RoleManager;
		this.userRoleManager = init.UserRoleManager;
		this.userLoginManager = init.UserLoginManager;
		this.auditLogManager = init.AuditLogManager;
		this.agentManager = init.AgentManager;
		this.minecraftVersions = init.MinecraftVersions;
		this.eventLogManager = init.EventLogManager;

		var senderActorInit = new WebMessageDataUpdateSenderActor.Init(connection, controllerState, init.InstanceLogManager);
		Context.ActorOf(WebMessageDataUpdateSenderActor.Factory(senderActorInit), "DataUpdateSender");
		
		ReceiveAsync<RegisterWebMessage>(HandleRegisterWeb);
		Receive<UnregisterWebMessage>(HandleUnregisterWeb);
		ReceiveAndReplyLater<CreateOrUpdateAdministratorUserMessage, CreateOrUpdateAdministratorUserResult>(HandleCreateOrUpdateAdministratorUser);
		ReceiveAndReplyLater<CreateUserMessage, CreateUserResult>(HandleCreateUser);
		ReceiveAndReplyLater<GetUsersMessage, ImmutableArray<UserInfo>>(HandleGetUsers);
		ReceiveAndReplyLater<GetRolesMessage, ImmutableArray<RoleInfo>>(HandleGetRoles);
		ReceiveAndReplyLater<GetUserRolesMessage, ImmutableDictionary<Guid, ImmutableArray<Guid>>>(HandleGetUserRoles);
		ReceiveAndReplyLater<ChangeUserRolesMessage, ChangeUserRolesResult>(HandleChangeUserRoles);
		ReceiveAndReplyLater<DeleteUserMessage, DeleteUserResult>(HandleDeleteUser);
		ReceiveAndReplyLater<CreateOrUpdateInstanceMessage, InstanceActionResult<CreateOrUpdateInstanceResult>>(HandleCreateOrUpdateInstance);
		ReceiveAndReplyLater<LaunchInstanceMessage, InstanceActionResult<LaunchInstanceResult>>(HandleLaunchInstance);
		ReceiveAndReplyLater<StopInstanceMessage, InstanceActionResult<StopInstanceResult>>(HandleStopInstance);
		ReceiveAndReplyLater<SendCommandToInstanceMessage, InstanceActionResult<SendCommandToInstanceResult>>(HandleSendCommandToInstance);
		ReceiveAndReplyLater<GetMinecraftVersionsMessage, ImmutableArray<MinecraftVersion>>(HandleGetMinecraftVersions); 
		ReceiveAndReply<GetAgentJavaRuntimesMessage, ImmutableDictionary<Guid, ImmutableArray<TaggedJavaRuntime>>>(HandleGetAgentJavaRuntimes);
		ReceiveAndReplyLater<GetAuditLogMessage, ImmutableArray<AuditLogItem>>(HandleGetAuditLog);
		ReceiveAndReplyLater<GetEventLogMessage, ImmutableArray<EventLogItem>>(HandleGetEventLog);
		ReceiveAndReplyLater<LogInMessage, LogInSuccess?>(HandleLogIn);
		Receive<ReplyMessage>(HandleReply);
	}

	private async Task HandleRegisterWeb(RegisterWebMessage message) {
		await webRegistrationHandler.TryRegisterImpl(connection, message);
	}

	private void HandleUnregisterWeb(UnregisterWebMessage message) {
		connection.Close();
	}

	private Task<CreateOrUpdateAdministratorUserResult> HandleCreateOrUpdateAdministratorUser(CreateOrUpdateAdministratorUserMessage message) {
		return userManager.CreateOrUpdateAdministrator(message.Username, message.Password);
	}

	private Task<CreateUserResult> HandleCreateUser(CreateUserMessage message) {
		return userManager.Create(message.LoggedInUserGuid, message.Username, message.Password);
	}

	private Task<ImmutableArray<UserInfo>> HandleGetUsers(GetUsersMessage message) {
		return userManager.GetAll();
	}

	private Task<ImmutableArray<RoleInfo>> HandleGetRoles(GetRolesMessage message) {
		return roleManager.GetAll();
	}

	private Task<ImmutableDictionary<Guid, ImmutableArray<Guid>>> HandleGetUserRoles(GetUserRolesMessage message) {
		return userRoleManager.GetUserRoles(message.UserGuids);
	}

	private Task<ChangeUserRolesResult> HandleChangeUserRoles(ChangeUserRolesMessage message) {
		return userRoleManager.ChangeUserRoles(message.LoggedInUserGuid, message.SubjectUserGuid, message.AddToRoleGuids, message.RemoveFromRoleGuids);
	}

	private Task<DeleteUserResult> HandleDeleteUser(DeleteUserMessage message) {
		return userManager.DeleteByGuid(message.LoggedInUserGuid, message.SubjectUserGuid);
	}

	private Task<InstanceActionResult<CreateOrUpdateInstanceResult>> HandleCreateOrUpdateInstance(CreateOrUpdateInstanceMessage message) {
		return agentManager.DoInstanceAction<AgentActor.CreateOrUpdateInstanceCommand, CreateOrUpdateInstanceResult>(message.Configuration.AgentGuid, new AgentActor.CreateOrUpdateInstanceCommand(message.LoggedInUserGuid, message.InstanceGuid, message.Configuration));
	}

	private Task<InstanceActionResult<LaunchInstanceResult>> HandleLaunchInstance(LaunchInstanceMessage message) {
		return agentManager.DoInstanceAction<AgentActor.LaunchInstanceCommand, LaunchInstanceResult>(message.AgentGuid, new AgentActor.LaunchInstanceCommand(message.InstanceGuid, message.LoggedInUserGuid));
	}

	private Task<InstanceActionResult<StopInstanceResult>> HandleStopInstance(StopInstanceMessage message) {
		return agentManager.DoInstanceAction<AgentActor.StopInstanceCommand, StopInstanceResult>(message.AgentGuid, new AgentActor.StopInstanceCommand(message.InstanceGuid, message.LoggedInUserGuid, message.StopStrategy));
	}

	private Task<InstanceActionResult<SendCommandToInstanceResult>> HandleSendCommandToInstance(SendCommandToInstanceMessage message) {
		return agentManager.DoInstanceAction<AgentActor.SendCommandToInstanceCommand, SendCommandToInstanceResult>(message.AgentGuid, new AgentActor.SendCommandToInstanceCommand(message.InstanceGuid, message.LoggedInUserGuid, message.Command));
	}

	private Task<ImmutableArray<MinecraftVersion>> HandleGetMinecraftVersions(GetMinecraftVersionsMessage message) {
		return minecraftVersions.GetVersions(CancellationToken.None);
	}

	private ImmutableDictionary<Guid, ImmutableArray<TaggedJavaRuntime>> HandleGetAgentJavaRuntimes(GetAgentJavaRuntimesMessage message) {
		return controllerState.AgentJavaRuntimesByGuid;
	}

	private Task<ImmutableArray<AuditLogItem>> HandleGetAuditLog(GetAuditLogMessage message) {
		return auditLogManager.GetMostRecentItems(message.Count);
	}

	private Task<ImmutableArray<EventLogItem>> HandleGetEventLog(GetEventLogMessage message) {
		return eventLogManager.GetMostRecentItems(message.Count);
	}

	private Task<LogInSuccess?> HandleLogIn(LogInMessage message) {
		return userLoginManager.LogIn(message.Username, message.Password);
	}

	private void HandleReply(ReplyMessage message) {
		connection.Receive(message);
	}
}
