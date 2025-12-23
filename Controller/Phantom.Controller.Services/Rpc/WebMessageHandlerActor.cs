using System.Collections.Immutable;
using Phantom.Common.Data;
using Phantom.Common.Data.Java;
using Phantom.Common.Data.Minecraft;
using Phantom.Common.Data.Replies;
using Phantom.Common.Data.Web.Agent;
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
		
		ReceiveAndReplyLater<LogInMessage, Optional<LogInSuccess>>(LogIn);
		Receive<LogOutMessage>(LogOut);
		ReceiveAndReply<GetAuthenticatedUser, Optional<AuthenticatedUserInfo>>(GetAuthenticatedUser);
		ReceiveAndReplyLater<CreateOrUpdateAdministratorUserMessage, CreateOrUpdateAdministratorUserResult>(CreateOrUpdateAdministratorUser);
		ReceiveAndReplyLater<CreateUserMessage, Result<CreateUserResult, UserActionFailure>>(CreateUser);
		ReceiveAndReplyLater<GetUsersMessage, ImmutableArray<UserInfo>>(GetUsers);
		ReceiveAndReplyLater<GetRolesMessage, ImmutableArray<RoleInfo>>(GetRoles);
		ReceiveAndReplyLater<GetUserRolesMessage, ImmutableDictionary<Guid, ImmutableArray<Guid>>>(GetUserRoles);
		ReceiveAndReplyLater<ChangeUserRolesMessage, Result<ChangeUserRolesResult, UserActionFailure>>(ChangeUserRoles);
		ReceiveAndReplyLater<DeleteUserMessage, Result<DeleteUserResult, UserActionFailure>>(DeleteUser);
		ReceiveAndReply<CreateOrUpdateAgentMessage, Result<CreateOrUpdateAgentResult, UserActionFailure>>(CreateOrUpdateAgentMessage);
		ReceiveAndReply<GetAgentJavaRuntimesMessage, ImmutableDictionary<Guid, ImmutableArray<TaggedJavaRuntime>>>(GetAgentJavaRuntimes);
		ReceiveAndReplyLater<CreateOrUpdateInstanceMessage, Result<CreateOrUpdateInstanceResult, UserInstanceActionFailure>>(CreateOrUpdateInstance);
		ReceiveAndReplyLater<LaunchInstanceMessage, Result<LaunchInstanceResult, UserInstanceActionFailure>>(LaunchInstance);
		ReceiveAndReplyLater<StopInstanceMessage, Result<StopInstanceResult, UserInstanceActionFailure>>(StopInstance);
		ReceiveAndReplyLater<SendCommandToInstanceMessage, Result<SendCommandToInstanceResult, UserInstanceActionFailure>>(SendCommandToInstance);
		ReceiveAndReplyLater<GetMinecraftVersionsMessage, ImmutableArray<MinecraftVersion>>(GetMinecraftVersions);
		ReceiveAndReplyLater<GetAuditLogMessage, Result<ImmutableArray<AuditLogItem>, UserActionFailure>>(GetAuditLog);
		ReceiveAndReplyLater<GetEventLogMessage, Result<ImmutableArray<EventLogItem>, UserActionFailure>>(GetEventLog);
	}
	
	private Task<Optional<LogInSuccess>> LogIn(LogInMessage message) {
		return userLoginManager.LogIn(message.Username, message.Password);
	}
	
	private void LogOut(LogOutMessage message) {
		_ = userLoginManager.LogOut(message.UserGuid, message.SessionToken);
	}
	
	private Optional<AuthenticatedUserInfo> GetAuthenticatedUser(GetAuthenticatedUser message) {
		return userLoginManager.GetAuthenticatedUser(message.UserGuid, message.AuthToken);
	}
	
	private Task<CreateOrUpdateAdministratorUserResult> CreateOrUpdateAdministratorUser(CreateOrUpdateAdministratorUserMessage message) {
		return userManager.CreateOrUpdateAdministrator(message.Username, message.Password);
	}
	
	private Task<Result<CreateUserResult, UserActionFailure>> CreateUser(CreateUserMessage message) {
		return userManager.Create(userLoginManager.GetLoggedInUser(message.AuthToken), message.Username, message.Password);
	}
	
	private Task<ImmutableArray<UserInfo>> GetUsers(GetUsersMessage message) {
		return userManager.GetAll();
	}
	
	private Task<ImmutableArray<RoleInfo>> GetRoles(GetRolesMessage message) {
		return roleManager.GetAll();
	}
	
	private Task<ImmutableDictionary<Guid, ImmutableArray<Guid>>> GetUserRoles(GetUserRolesMessage message) {
		return userRoleManager.GetUserRoles(message.UserGuids);
	}
	
	private Task<Result<ChangeUserRolesResult, UserActionFailure>> ChangeUserRoles(ChangeUserRolesMessage message) {
		return userRoleManager.ChangeUserRoles(userLoginManager.GetLoggedInUser(message.AuthToken), message.SubjectUserGuid, message.AddToRoleGuids, message.RemoveFromRoleGuids);
	}
	
	private Task<Result<DeleteUserResult, UserActionFailure>> DeleteUser(DeleteUserMessage message) {
		return userManager.DeleteByGuid(userLoginManager.GetLoggedInUser(message.AuthToken), message.SubjectUserGuid);
	}
	
	private Result<CreateOrUpdateAgentResult, UserActionFailure> CreateOrUpdateAgentMessage(CreateOrUpdateAgentMessage message) {
		return agentManager.CreateOrUpdateAgent(userLoginManager.GetLoggedInUser(message.AuthToken), message.AgentGuid, message.Configuration);
	}
	
	private ImmutableDictionary<Guid, ImmutableArray<TaggedJavaRuntime>> GetAgentJavaRuntimes(GetAgentJavaRuntimesMessage message) {
		return controllerState.AgentJavaRuntimesByGuid;
	}
	
	private Task<Result<CreateOrUpdateInstanceResult, UserInstanceActionFailure>> CreateOrUpdateInstance(CreateOrUpdateInstanceMessage message) {
		return agentManager.DoInstanceAction<AgentActor.CreateOrUpdateInstanceCommand, CreateOrUpdateInstanceResult>(
			userLoginManager.GetLoggedInUser(message.AuthToken),
			Permission.CreateInstances,
			message.Configuration.AgentGuid,
			loggedInUserGuid => new AgentActor.CreateOrUpdateInstanceCommand(loggedInUserGuid, message.InstanceGuid, message.Configuration)
		);
	}
	
	private Task<Result<LaunchInstanceResult, UserInstanceActionFailure>> LaunchInstance(LaunchInstanceMessage message) {
		return agentManager.DoInstanceAction<AgentActor.LaunchInstanceCommand, LaunchInstanceResult>(
			userLoginManager.GetLoggedInUser(message.AuthToken),
			Permission.ControlInstances,
			message.AgentGuid,
			loggedInUserGuid => new AgentActor.LaunchInstanceCommand(loggedInUserGuid, message.InstanceGuid)
		);
	}
	
	private Task<Result<StopInstanceResult, UserInstanceActionFailure>> StopInstance(StopInstanceMessage message) {
		return agentManager.DoInstanceAction<AgentActor.StopInstanceCommand, StopInstanceResult>(
			userLoginManager.GetLoggedInUser(message.AuthToken),
			Permission.ControlInstances,
			message.AgentGuid,
			loggedInUserGuid => new AgentActor.StopInstanceCommand(loggedInUserGuid, message.InstanceGuid, message.StopStrategy)
		);
	}
	
	private Task<Result<SendCommandToInstanceResult, UserInstanceActionFailure>> SendCommandToInstance(SendCommandToInstanceMessage message) {
		return agentManager.DoInstanceAction<AgentActor.SendCommandToInstanceCommand, SendCommandToInstanceResult>(
			userLoginManager.GetLoggedInUser(message.AuthToken),
			Permission.ControlInstances,
			message.AgentGuid,
			loggedInUserGuid => new AgentActor.SendCommandToInstanceCommand(loggedInUserGuid, message.InstanceGuid, message.Command)
		);
	}
	
	private Task<ImmutableArray<MinecraftVersion>> GetMinecraftVersions(GetMinecraftVersionsMessage message) {
		return minecraftVersions.GetVersions(CancellationToken.None);
	}
	
	private Task<Result<ImmutableArray<AuditLogItem>, UserActionFailure>> GetAuditLog(GetAuditLogMessage message) {
		return auditLogManager.GetMostRecentItems(userLoginManager.GetLoggedInUser(message.AuthToken), message.Count);
	}
	
	private Task<Result<ImmutableArray<EventLogItem>, UserActionFailure>> GetEventLog(GetEventLogMessage message) {
		return eventLogManager.GetMostRecentItems(userLoginManager.GetLoggedInUser(message.AuthToken), message.Count);
	}
}
