using Phantom.Common.Data;
using Phantom.Common.Messages.Agent;
using Phantom.Common.Messages.Web;
using Phantom.Controller.Database;
using Phantom.Controller.Minecraft;
using Phantom.Controller.Rpc;
using Phantom.Controller.Services.Agents;
using Phantom.Controller.Services.Events;
using Phantom.Controller.Services.Instances;
using Phantom.Controller.Services.Rpc;
using Phantom.Controller.Services.Users;
using Phantom.Utils.Logging;
using Phantom.Utils.Tasks;

namespace Phantom.Controller.Services;

public sealed class ControllerServices {
	private TaskManager TaskManager { get; }
	private MinecraftVersions MinecraftVersions { get; }

	private AgentManager AgentManager { get; }
	private AgentJavaRuntimesManager AgentJavaRuntimesManager { get; }
	private InstanceManager InstanceManager { get; }
	private InstanceLogManager InstanceLogManager { get; }
	private EventLogManager EventLogManager { get; }

	private UserManager UserManager { get; }
	private RoleManager RoleManager { get; }
	private PermissionManager PermissionManager { get; }

	private UserRoleManager UserRoleManager { get; }
	private UserLoginManager UserLoginManager { get; }
	private AuditLogManager AuditLogManager { get; }
	
	private readonly IDbContextProvider dbProvider;
	private readonly AuthToken webAuthToken;
	private readonly CancellationToken cancellationToken;
	
	public ControllerServices(IDbContextProvider dbProvider, AuthToken agentAuthToken, AuthToken webAuthToken, CancellationToken shutdownCancellationToken) {
		this.TaskManager = new TaskManager(PhantomLogger.Create<TaskManager, ControllerServices>());
		this.MinecraftVersions = new MinecraftVersions();
		
		this.AgentManager = new AgentManager(agentAuthToken, dbProvider, TaskManager, shutdownCancellationToken);
		this.AgentJavaRuntimesManager = new AgentJavaRuntimesManager();
		this.InstanceManager = new InstanceManager(AgentManager, MinecraftVersions, dbProvider, shutdownCancellationToken);
		this.InstanceLogManager = new InstanceLogManager();
		
		this.UserManager = new UserManager(dbProvider);
		this.RoleManager = new RoleManager(dbProvider);
		this.PermissionManager = new PermissionManager(dbProvider);

		this.UserRoleManager = new UserRoleManager(dbProvider);
		this.UserLoginManager = new UserLoginManager(UserManager, PermissionManager);
		this.AuditLogManager = new AuditLogManager(dbProvider);
		this.EventLogManager = new EventLogManager(dbProvider, TaskManager, shutdownCancellationToken);
		
		this.dbProvider = dbProvider;
		this.webAuthToken = webAuthToken;
		this.cancellationToken = shutdownCancellationToken;
	}

	public AgentMessageListener CreateAgentMessageListener(RpcConnectionToClient<IMessageToAgentListener> connection) {
		return new AgentMessageListener(connection, AgentManager, AgentJavaRuntimesManager, InstanceManager, InstanceLogManager, EventLogManager, cancellationToken);
	}

	public WebMessageListener CreateWebMessageListener(RpcConnectionToClient<IMessageToWebListener> connection) {
		return new WebMessageListener(connection, webAuthToken, UserManager, RoleManager, UserRoleManager, UserLoginManager, AuditLogManager, AgentManager, AgentJavaRuntimesManager, InstanceManager, InstanceLogManager, MinecraftVersions, EventLogManager, TaskManager);
	}

	public async Task Initialize() {
		await DatabaseMigrator.Run(dbProvider, cancellationToken);
		await PermissionManager.Initialize();
		await RoleManager.Initialize();
		await AgentManager.Initialize();
		await InstanceManager.Initialize();
	}
}
