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
using Phantom.Common.Messages.Web.ToController;
using Phantom.Common.Messages.Web.ToWeb;
using Phantom.Utils.Rpc.Message;

namespace Phantom.Common.Messages.Web;

public static class WebMessageRegistries {
	public static MessageRegistry<IMessageToController> ToController { get; } = new (nameof(ToController));
	public static MessageRegistry<IMessageToWeb> ToWeb { get; } = new (nameof(ToWeb));
	
	public static MessageRegistries<IMessageToController, IMessageToWeb> Registries => new (ToWeb, ToController);
	
	static WebMessageRegistries() {
		ToController.Add<LogInMessage, Optional<LogInSuccess>>();
		ToController.Add<LogOutMessage>();
		ToController.Add<GetAuthenticatedUser, Optional<AuthenticatedUserInfo>>();
		ToController.Add<CreateOrUpdateAdministratorUserMessage, CreateOrUpdateAdministratorUserResult>();
		ToController.Add<CreateUserMessage, Result<CreateUserResult, UserActionFailure>>();
		ToController.Add<GetUsersMessage, ImmutableArray<UserInfo>>();
		ToController.Add<GetRolesMessage, ImmutableArray<RoleInfo>>();
		ToController.Add<GetUserRolesMessage, ImmutableDictionary<Guid, ImmutableArray<Guid>>>();
		ToController.Add<ChangeUserRolesMessage, Result<ChangeUserRolesResult, UserActionFailure>>();
		ToController.Add<DeleteUserMessage, Result<DeleteUserResult, UserActionFailure>>();
		ToController.Add<CreateOrUpdateAgentMessage, Result<CreateOrUpdateAgentResult, UserActionFailure>>();
		ToController.Add<GetAgentJavaRuntimesMessage, ImmutableDictionary<Guid, ImmutableArray<TaggedJavaRuntime>>>();
		ToController.Add<CreateOrUpdateInstanceMessage, Result<CreateOrUpdateInstanceResult, UserInstanceActionFailure>>();
		ToController.Add<LaunchInstanceMessage, Result<LaunchInstanceResult, UserInstanceActionFailure>>();
		ToController.Add<StopInstanceMessage, Result<StopInstanceResult, UserInstanceActionFailure>>();
		ToController.Add<SendCommandToInstanceMessage, Result<SendCommandToInstanceResult, UserInstanceActionFailure>>();
		ToController.Add<GetMinecraftVersionsMessage, ImmutableArray<MinecraftVersion>>();
		ToController.Add<GetAuditLogMessage, Result<ImmutableArray<AuditLogItem>, UserActionFailure>>();
		ToController.Add<GetEventLogMessage, Result<ImmutableArray<EventLogItem>, UserActionFailure>>();
		
		ToWeb.Add<RefreshAgentsMessage>();
		ToWeb.Add<RefreshInstancesMessage>();
		ToWeb.Add<InstanceOutputMessage>();
		ToWeb.Add<RefreshUserSessionMessage>();
	}
}
