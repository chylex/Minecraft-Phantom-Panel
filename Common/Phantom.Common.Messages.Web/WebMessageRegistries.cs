using System.Collections.Immutable;
using Phantom.Common.Data;
using Phantom.Common.Data.Java;
using Phantom.Common.Data.Minecraft;
using Phantom.Common.Data.Replies;
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
	
	public static IMessageDefinitions<IMessageToController, IMessageToWeb> Definitions { get; } = new MessageDefinitions();
	
	static WebMessageRegistries() {
		ToController.Add<LogInMessage, Optional<LogInSuccess>>(1);
		ToController.Add<LogOutMessage>(2);
		ToController.Add<GetAuthenticatedUser, Optional<AuthenticatedUserInfo>>(3);
		ToController.Add<CreateOrUpdateAdministratorUserMessage, CreateOrUpdateAdministratorUserResult>(4);
		ToController.Add<CreateUserMessage, Result<CreateUserResult, UserActionFailure>>(5);
		ToController.Add<DeleteUserMessage, Result<DeleteUserResult, UserActionFailure>>(6);
		ToController.Add<GetUsersMessage, ImmutableArray<UserInfo>>(7);
		ToController.Add<GetRolesMessage, ImmutableArray<RoleInfo>>(8);
		ToController.Add<GetUserRolesMessage, ImmutableDictionary<Guid, ImmutableArray<Guid>>>(9);
		ToController.Add<ChangeUserRolesMessage, Result<ChangeUserRolesResult, UserActionFailure>>(10);
		ToController.Add<CreateOrUpdateInstanceMessage, Result<CreateOrUpdateInstanceResult, UserInstanceActionFailure>>(11);
		ToController.Add<LaunchInstanceMessage, Result<LaunchInstanceResult, UserInstanceActionFailure>>(12);
		ToController.Add<StopInstanceMessage, Result<StopInstanceResult, UserInstanceActionFailure>>(13);
		ToController.Add<SendCommandToInstanceMessage, Result<SendCommandToInstanceResult, UserInstanceActionFailure>>(14);
		ToController.Add<GetMinecraftVersionsMessage, ImmutableArray<MinecraftVersion>>(15);
		ToController.Add<GetAgentJavaRuntimesMessage, ImmutableDictionary<Guid, ImmutableArray<TaggedJavaRuntime>>>(16);
		ToController.Add<GetAuditLogMessage, Result<ImmutableArray<AuditLogItem>, UserActionFailure>>(17);
		ToController.Add<GetEventLogMessage, Result<ImmutableArray<EventLogItem>, UserActionFailure>>(18);
		
		ToWeb.Add<RefreshAgentsMessage>(1);
		ToWeb.Add<RefreshInstancesMessage>(2);
		ToWeb.Add<InstanceOutputMessage>(3);
		ToWeb.Add<RefreshUserSessionMessage>(4);
	}
	
	private sealed class MessageDefinitions : IMessageDefinitions<IMessageToController, IMessageToWeb> {
		public MessageRegistry<IMessageToWeb> ToClient => ToWeb;
		public MessageRegistry<IMessageToController> ToServer => ToController;
	}
}
