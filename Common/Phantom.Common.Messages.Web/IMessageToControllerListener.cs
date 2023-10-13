using System.Collections.Immutable;
using Phantom.Common.Data.Java;
using Phantom.Common.Data.Minecraft;
using Phantom.Common.Data.Replies;
using Phantom.Common.Data.Web.AuditLog;
using Phantom.Common.Data.Web.EventLog;
using Phantom.Common.Data.Web.Instance;
using Phantom.Common.Data.Web.Users;
using Phantom.Common.Messages.Web.BiDirectional;
using Phantom.Common.Messages.Web.ToController;
using Phantom.Utils.Rpc.Message;

namespace Phantom.Common.Messages.Web;

public interface IMessageToControllerListener {
	Task<NoReply> HandleRegisterWeb(RegisterWebMessage message);
	Task<NoReply> HandleUnregisterWeb(UnregisterWebMessage message);
	Task<LogInSuccess?> HandleLogIn(LogInMessage message);
	Task<CreateOrUpdateAdministratorUserResult> HandleCreateOrUpdateAdministratorUser(CreateOrUpdateAdministratorUserMessage message);
	Task<CreateUserResult> HandleCreateUser(CreateUserMessage message);
	Task<ImmutableArray<UserInfo>> HandleGetUsers(GetUsersMessage message);
	Task<ImmutableArray<RoleInfo>> HandleGetRoles(GetRolesMessage message);
	Task<ImmutableDictionary<Guid, ImmutableArray<Guid>>> HandleGetUserRoles(GetUserRolesMessage message);
	Task<ChangeUserRolesResult> HandleChangeUserRoles(ChangeUserRolesMessage message);
	Task<DeleteUserResult> HandleDeleteUser(DeleteUserMessage message);
	Task<InstanceActionResult<CreateOrUpdateInstanceResult>> HandleCreateOrUpdateInstance(CreateOrUpdateInstanceMessage message);
	Task<InstanceActionResult<LaunchInstanceResult>> HandleLaunchInstance(LaunchInstanceMessage message);
	Task<InstanceActionResult<StopInstanceResult>> HandleStopInstance(StopInstanceMessage message);
	Task<InstanceActionResult<SendCommandToInstanceResult>> HandleSendCommandToInstance(SendCommandToInstanceMessage message);
	Task<ImmutableArray<MinecraftVersion>> HandleGetMinecraftVersions(GetMinecraftVersionsMessage message);
	Task<ImmutableDictionary<Guid, ImmutableArray<TaggedJavaRuntime>>> HandleGetAgentJavaRuntimes(GetAgentJavaRuntimesMessage message);
	Task<ImmutableArray<AuditLogItem>> HandleGetAuditLog(GetAuditLogMessage message);
	Task<ImmutableArray<EventLogItem>> HandleGetEventLog(GetEventLogMessage message);
	Task<NoReply> HandleReply(ReplyMessage message);
}
