using System.Collections.Immutable;
using Phantom.Common.Data;
using Phantom.Common.Data.Java;
using Phantom.Common.Data.Minecraft;
using Phantom.Common.Data.Replies;
using Phantom.Common.Data.Web.AuditLog;
using Phantom.Common.Data.Web.EventLog;
using Phantom.Common.Data.Web.Instance;
using Phantom.Common.Data.Web.Users;
using Phantom.Common.Messages.Web.BiDirectional;
using Phantom.Common.Messages.Web.ToController;
using Phantom.Common.Messages.Web.ToWeb;
using Phantom.Utils.Logging;
using Phantom.Utils.Rpc.Message;

namespace Phantom.Common.Messages.Web;

public static class WebMessageRegistries {
	public static MessageRegistry<IMessageToController> ToController { get; } = new (PhantomLogger.Create("MessageRegistry", nameof(ToController)));
	public static MessageRegistry<IMessageToWeb> ToWeb { get; } = new (PhantomLogger.Create("MessageRegistry", nameof(ToWeb)));
	
	public static IMessageDefinitions<IMessageToWeb, IMessageToController, ReplyMessage> Definitions { get; } = new MessageDefinitions();

	static WebMessageRegistries() {
		ToController.Add<RegisterWebMessage>(0);
		ToController.Add<UnregisterWebMessage>(1);
		ToController.Add<LogInMessage, LogInSuccess?>(2);
		ToController.Add<LogOutMessage>(3);
		ToController.Add<GetAuthenticatedUser, Optional<AuthenticatedUserInfo>>(4);
		ToController.Add<CreateOrUpdateAdministratorUserMessage, CreateOrUpdateAdministratorUserResult>(5);
		ToController.Add<CreateUserMessage, Result<CreateUserResult, UserActionFailure>>(6);
		ToController.Add<DeleteUserMessage, Result<DeleteUserResult, UserActionFailure>>(7);
		ToController.Add<GetUsersMessage, ImmutableArray<UserInfo>>(8);
		ToController.Add<GetRolesMessage, ImmutableArray<RoleInfo>>(9);
		ToController.Add<GetUserRolesMessage, ImmutableDictionary<Guid, ImmutableArray<Guid>>>(10);
		ToController.Add<ChangeUserRolesMessage, Result<ChangeUserRolesResult, UserActionFailure>>(11);
		ToController.Add<CreateOrUpdateInstanceMessage, Result<CreateOrUpdateInstanceResult, UserInstanceActionFailure>>(12);
		ToController.Add<LaunchInstanceMessage, Result<LaunchInstanceResult, UserInstanceActionFailure>>(13);
		ToController.Add<StopInstanceMessage, Result<StopInstanceResult, UserInstanceActionFailure>>(14);
		ToController.Add<SendCommandToInstanceMessage, Result<SendCommandToInstanceResult, UserInstanceActionFailure>>(15);
		ToController.Add<GetMinecraftVersionsMessage, ImmutableArray<MinecraftVersion>>(16);
		ToController.Add<GetAgentJavaRuntimesMessage, ImmutableDictionary<Guid, ImmutableArray<TaggedJavaRuntime>>>(17);
		ToController.Add<GetAuditLogMessage, Result<ImmutableArray<AuditLogItem>, UserActionFailure>>(18);
		ToController.Add<GetEventLogMessage, Result<ImmutableArray<EventLogItem>, UserActionFailure>>(19);
		ToController.Add<ReplyMessage>(127);
		
		ToWeb.Add<RegisterWebResultMessage>(0);
		ToWeb.Add<RefreshAgentsMessage>(1);
		ToWeb.Add<RefreshInstancesMessage>(2);
		ToWeb.Add<InstanceOutputMessage>(3);
		ToWeb.Add<RefreshUserSessionMessage>(4);
		ToWeb.Add<ReplyMessage>(127);
	}

	private sealed class MessageDefinitions : IMessageDefinitions<IMessageToWeb, IMessageToController, ReplyMessage> {
		public MessageRegistry<IMessageToWeb> ToClient => ToWeb;
		public MessageRegistry<IMessageToController> ToServer => ToController;

		public ReplyMessage CreateReplyMessage(uint sequenceId, byte[] serializedReply) {
			return new ReplyMessage(sequenceId, serializedReply);
		}
	}
}
