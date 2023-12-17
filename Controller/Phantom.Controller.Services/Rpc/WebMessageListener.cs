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
using Phantom.Common.Messages.Web.BiDirectional;
using Phantom.Common.Messages.Web.ToController;
using Phantom.Common.Messages.Web.ToWeb;
using Phantom.Controller.Minecraft;
using Phantom.Controller.Services.Agents;
using Phantom.Controller.Services.Events;
using Phantom.Controller.Services.Instances;
using Phantom.Controller.Services.Users;
using Phantom.Utils.Logging;
using Phantom.Utils.Rpc.Message;
using Phantom.Utils.Rpc.Runtime;
using Phantom.Utils.Tasks;
using Serilog;

namespace Phantom.Controller.Services.Rpc;

public sealed class WebMessageListener : IMessageToControllerListener {
	private static readonly ILogger Logger = PhantomLogger.Create<WebMessageListener>();
	
	private readonly RpcConnectionToClient<IMessageToWebListener> connection;
	private readonly AuthToken authToken;
	private readonly UserManager userManager;
	private readonly RoleManager roleManager;
	private readonly UserRoleManager userRoleManager;
	private readonly UserLoginManager userLoginManager;
	private readonly AuditLogManager auditLogManager;
	private readonly AgentManager agentManager;
	private readonly AgentJavaRuntimesManager agentJavaRuntimesManager;
	private readonly InstanceManager instanceManager;
	private readonly InstanceLogManager instanceLogManager;
	private readonly MinecraftVersions minecraftVersions;
	private readonly EventLogManager eventLogManager;
	private readonly TaskManager taskManager;

	internal WebMessageListener(
		RpcConnectionToClient<IMessageToWebListener> connection,
		AuthToken authToken,
		UserManager userManager,
		RoleManager roleManager,
		UserRoleManager userRoleManager,
		UserLoginManager userLoginManager,
		AuditLogManager auditLogManager,
		AgentManager agentManager,
		AgentJavaRuntimesManager agentJavaRuntimesManager,
		InstanceManager instanceManager,
		InstanceLogManager instanceLogManager,
		MinecraftVersions minecraftVersions,
		EventLogManager eventLogManager,
		TaskManager taskManager
	) {
		this.connection = connection;
		this.authToken = authToken;
		this.userManager = userManager;
		this.roleManager = roleManager;
		this.userRoleManager = userRoleManager;
		this.userLoginManager = userLoginManager;
		this.auditLogManager = auditLogManager;
		this.agentManager = agentManager;
		this.agentJavaRuntimesManager = agentJavaRuntimesManager;
		this.instanceManager = instanceManager;
		this.instanceLogManager = instanceLogManager;
		this.minecraftVersions = minecraftVersions;
		this.eventLogManager = eventLogManager;
		this.taskManager = taskManager;
	}

	private void OnConnectionReady() {
		lock (this) {
			agentManager.AgentsChanged.Subscribe(this, HandleAgentsChanged);
			instanceManager.InstancesChanged.Subscribe(this, HandleInstancesChanged);
			instanceLogManager.LogsReceived += HandleInstanceLogsReceived;
		}
	}

	private void OnConnectionClosed() {
		lock (this) {
			agentManager.AgentsChanged.Unsubscribe(this);
			instanceManager.InstancesChanged.Unsubscribe(this);
			instanceLogManager.LogsReceived -= HandleInstanceLogsReceived;
		}
	}

	private void HandleAgentsChanged(ImmutableArray<Agent> agents) {
		var message = new RefreshAgentsMessage(agents.Select(static agent => new AgentWithStats(agent.Guid, agent.Name, agent.ProtocolVersion, agent.BuildVersion, agent.MaxInstances, agent.MaxMemory, agent.AllowedServerPorts, agent.AllowedRconPorts, agent.Stats, agent.LastPing, agent.IsOnline)).ToImmutableArray());
		taskManager.Run("Send agents to web", () => connection.Send(message));
	}

	private void HandleInstancesChanged(ImmutableDictionary<Guid, Instance> instances) {
		var message = new RefreshInstancesMessage(instances.Values.ToImmutableArray());
		taskManager.Run("Send instances to web", () => connection.Send(message));
	}

	private void HandleInstanceLogsReceived(object? sender, InstanceLogManager.Event e) {
		taskManager.Run("Send instance logs to web", () => connection.Send(new InstanceOutputMessage(e.InstanceGuid, e.Lines)));
	}

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
			OnConnectionReady();
		}

		return NoReply.Instance;
	}

	public Task<NoReply> HandleUnregisterWeb(UnregisterWebMessage message) {
		if (!connection.IsClosed) {
			connection.Close();
			OnConnectionClosed();
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
		return instanceManager.CreateOrUpdateInstance(message.LoggedInUserGuid, message.Configuration);
	}

	public Task<InstanceActionResult<LaunchInstanceResult>> HandleLaunchInstance(LaunchInstanceMessage message) {
		return instanceManager.LaunchInstance(message.LoggedInUserGuid, message.InstanceGuid);
	}

	public Task<InstanceActionResult<StopInstanceResult>> HandleStopInstance(StopInstanceMessage message) {
		return instanceManager.StopInstance(message.LoggedInUserGuid, message.InstanceGuid, message.StopStrategy);
	}

	public Task<InstanceActionResult<SendCommandToInstanceResult>> HandleSendCommandToInstance(SendCommandToInstanceMessage message) {
		return instanceManager.SendCommand(message.LoggedInUserGuid, message.InstanceGuid, message.Command);
	}

	public Task<ImmutableArray<MinecraftVersion>> HandleGetMinecraftVersions(GetMinecraftVersionsMessage message) {
		return minecraftVersions.GetVersions(CancellationToken.None);
	}

	public Task<ImmutableDictionary<Guid, ImmutableArray<TaggedJavaRuntime>>> HandleGetAgentJavaRuntimes(GetAgentJavaRuntimesMessage message) {
		return Task.FromResult(agentJavaRuntimesManager.All);
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
