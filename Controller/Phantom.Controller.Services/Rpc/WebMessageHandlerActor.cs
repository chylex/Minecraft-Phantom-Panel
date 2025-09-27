using System.Collections.Immutable;
using Phantom.Common.Data;
using Phantom.Common.Data.Java;
using Phantom.Common.Data.Minecraft;
using Phantom.Common.Data.Replies;
using Phantom.Common.Data.Web.AuditLog;
using Phantom.Common.Data.Web.EventLog;
using Phantom.Common.Data.Web.Instance;
using Phantom.Common.Data.Web.Users;
using Phantom.Common.Messages.Web;
using Phantom.Common.Messages.Web.ToController;
using Phantom.Controller.Minecraft;
using Phantom.Controller.Services.Agents;
using Phantom.Controller.Services.Events;
using Phantom.Controller.Services.Instances;
using Phantom.Controller.Services.Users;
using Phantom.Controller.Services.Users.Sessions;
using Phantom.Utils.Actor;
using Phantom.Utils.Rpc.Runtime.Server;

namespace Phantom.Controller.Services.Rpc;

sealed class WebMessageHandlerActor : ReceiveActor<IMessageToController> {
	public readonly record struct Init(
		RpcServerToClientConnection<IMessageToController, IMessageToWeb> Connection,
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
		this.controllerState = init.ControllerState;
		this.userManager = init.UserManager;
		this.roleManager = init.RoleManager;
		this.userRoleManager = init.UserRoleManager;
		this.userLoginManager = init.UserLoginManager;
		this.auditLogManager = init.AuditLogManager;
		this.agentManager = init.AgentManager;
		this.minecraftVersions = init.MinecraftVersions;
		this.eventLogManager = init.EventLogManager;
		
		var senderActorInit = new WebMessageDataUpdateSenderActor.Init(init.Connection.MessageSender, controllerState, init.InstanceLogManager);
		Context.ActorOf(WebMessageDataUpdateSenderActor.Factory(senderActorInit), "DataUpdateSender");
		
		ReceiveAndReplyLater<LogInMessage, Optional<LogInSuccess>>(HandleLogIn);
		Receive<LogOutMessage>(HandleLogOut);
		ReceiveAndReply<GetAuthenticatedUser, Optional<AuthenticatedUserInfo>>(GetAuthenticatedUser);
		ReceiveAndReplyLater<CreateOrUpdateAdministratorUserMessage, CreateOrUpdateAdministratorUserResult>(HandleCreateOrUpdateAdministratorUser);
		ReceiveAndReplyLater<CreateUserMessage, Result<CreateUserResult, UserActionFailure>>(HandleCreateUser);
		ReceiveAndReplyLater<GetUsersMessage, ImmutableArray<UserInfo>>(HandleGetUsers);
		ReceiveAndReplyLater<GetRolesMessage, ImmutableArray<RoleInfo>>(HandleGetRoles);
		ReceiveAndReplyLater<GetUserRolesMessage, ImmutableDictionary<Guid, ImmutableArray<Guid>>>(HandleGetUserRoles);
		ReceiveAndReplyLater<ChangeUserRolesMessage, Result<ChangeUserRolesResult, UserActionFailure>>(HandleChangeUserRoles);
		ReceiveAndReplyLater<DeleteUserMessage, Result<DeleteUserResult, UserActionFailure>>(HandleDeleteUser);
		ReceiveAndReplyLater<CreateOrUpdateInstanceMessage, Result<CreateOrUpdateInstanceResult, UserInstanceActionFailure>>(HandleCreateOrUpdateInstance);
		ReceiveAndReplyLater<LaunchInstanceMessage, Result<LaunchInstanceResult, UserInstanceActionFailure>>(HandleLaunchInstance);
		ReceiveAndReplyLater<StopInstanceMessage, Result<StopInstanceResult, UserInstanceActionFailure>>(HandleStopInstance);
		ReceiveAndReplyLater<SendCommandToInstanceMessage, Result<SendCommandToInstanceResult, UserInstanceActionFailure>>(HandleSendCommandToInstance);
		ReceiveAndReplyLater<GetMinecraftVersionsMessage, ImmutableArray<MinecraftVersion>>(HandleGetMinecraftVersions);
		ReceiveAndReply<GetAgentJavaRuntimesMessage, ImmutableDictionary<Guid, ImmutableArray<TaggedJavaRuntime>>>(HandleGetAgentJavaRuntimes);
		ReceiveAndReplyLater<GetAuditLogMessage, Result<ImmutableArray<AuditLogItem>, UserActionFailure>>(HandleGetAuditLog);
		ReceiveAndReplyLater<GetEventLogMessage, Result<ImmutableArray<EventLogItem>, UserActionFailure>>(HandleGetEventLog);
	}
	
	private Task<Optional<LogInSuccess>> HandleLogIn(LogInMessage message) {
		return userLoginManager.LogIn(message.Username, message.Password);
	}
	
	private void HandleLogOut(LogOutMessage message) {
		_ = userLoginManager.LogOut(message.UserGuid, message.SessionToken);
	}
	
	private Optional<AuthenticatedUserInfo> GetAuthenticatedUser(GetAuthenticatedUser message) {
		return userLoginManager.GetAuthenticatedUser(message.UserGuid, message.AuthToken);
	}
	
	private Task<CreateOrUpdateAdministratorUserResult> HandleCreateOrUpdateAdministratorUser(CreateOrUpdateAdministratorUserMessage message) {
		return userManager.CreateOrUpdateAdministrator(message.Username, message.Password);
	}
	
	private Task<Result<CreateUserResult, UserActionFailure>> HandleCreateUser(CreateUserMessage message) {
		return userManager.Create(userLoginManager.GetLoggedInUser(message.AuthToken), message.Username, message.Password);
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
	
	private Task<Result<ChangeUserRolesResult, UserActionFailure>> HandleChangeUserRoles(ChangeUserRolesMessage message) {
		return userRoleManager.ChangeUserRoles(userLoginManager.GetLoggedInUser(message.AuthToken), message.SubjectUserGuid, message.AddToRoleGuids, message.RemoveFromRoleGuids);
	}
	
	private Task<Result<DeleteUserResult, UserActionFailure>> HandleDeleteUser(DeleteUserMessage message) {
		return userManager.DeleteByGuid(userLoginManager.GetLoggedInUser(message.AuthToken), message.SubjectUserGuid);
	}
	
	private Task<Result<CreateOrUpdateInstanceResult, UserInstanceActionFailure>> HandleCreateOrUpdateInstance(CreateOrUpdateInstanceMessage message) {
		return agentManager.DoInstanceAction<AgentActor.CreateOrUpdateInstanceCommand, CreateOrUpdateInstanceResult>(
			Permission.CreateInstances,
			message.AuthToken,
			message.Configuration.AgentGuid,
			loggedInUserGuid => new AgentActor.CreateOrUpdateInstanceCommand(loggedInUserGuid, message.InstanceGuid, message.Configuration)
		);
	}
	
	private Task<Result<LaunchInstanceResult, UserInstanceActionFailure>> HandleLaunchInstance(LaunchInstanceMessage message) {
		return agentManager.DoInstanceAction<AgentActor.LaunchInstanceCommand, LaunchInstanceResult>(
			Permission.ControlInstances,
			message.AuthToken,
			message.AgentGuid,
			loggedInUserGuid => new AgentActor.LaunchInstanceCommand(loggedInUserGuid, message.InstanceGuid)
		);
	}
	
	private Task<Result<StopInstanceResult, UserInstanceActionFailure>> HandleStopInstance(StopInstanceMessage message) {
		return agentManager.DoInstanceAction<AgentActor.StopInstanceCommand, StopInstanceResult>(
			Permission.ControlInstances,
			message.AuthToken,
			message.AgentGuid,
			loggedInUserGuid => new AgentActor.StopInstanceCommand(loggedInUserGuid, message.InstanceGuid, message.StopStrategy)
		);
	}
	
	private Task<Result<SendCommandToInstanceResult, UserInstanceActionFailure>> HandleSendCommandToInstance(SendCommandToInstanceMessage message) {
		return agentManager.DoInstanceAction<AgentActor.SendCommandToInstanceCommand, SendCommandToInstanceResult>(
			Permission.ControlInstances,
			message.AuthToken,
			message.AgentGuid,
			loggedInUserGuid => new AgentActor.SendCommandToInstanceCommand(loggedInUserGuid, message.InstanceGuid, message.Command)
		);
	}
	
	private Task<ImmutableArray<MinecraftVersion>> HandleGetMinecraftVersions(GetMinecraftVersionsMessage message) {
		return minecraftVersions.GetVersions(CancellationToken.None);
	}
	
	private ImmutableDictionary<Guid, ImmutableArray<TaggedJavaRuntime>> HandleGetAgentJavaRuntimes(GetAgentJavaRuntimesMessage message) {
		return controllerState.AgentJavaRuntimesByGuid;
	}
	
	private Task<Result<ImmutableArray<AuditLogItem>, UserActionFailure>> HandleGetAuditLog(GetAuditLogMessage message) {
		return auditLogManager.GetMostRecentItems(userLoginManager.GetLoggedInUser(message.AuthToken), message.Count);
	}
	
	private Task<Result<ImmutableArray<EventLogItem>, UserActionFailure>> HandleGetEventLog(GetEventLogMessage message) {
		return eventLogManager.GetMostRecentItems(userLoginManager.GetLoggedInUser(message.AuthToken), message.Count);
	}
}
