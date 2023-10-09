using Phantom.Common.Data.Agent;
using Phantom.Common.Logging;
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
using Phantom.Controller.Services.Users.Permissions;
using Phantom.Controller.Services.Users.Roles;
using Phantom.Utils.Tasks;

namespace Phantom.Controller.Services;

public sealed class ControllerServices {
	private TaskManager TaskManager { get; }
	private MinecraftVersions MinecraftVersions { get; }
	
	private AgentManager AgentManager { get; }
	private AgentJavaRuntimesManager AgentJavaRuntimesManager { get; }
	private EventLog EventLog { get; }
	private InstanceManager InstanceManager { get; }
	private InstanceLogManager InstanceLogManager { get; }
	
	private UserManager UserManager { get; }
	private RoleManager RoleManager { get; }
	private UserRoleManager UserRoleManager { get; }
	private PermissionManager PermissionManager { get; }

	private readonly IDatabaseProvider databaseProvider;
	private readonly CancellationToken cancellationToken;
	
	public ControllerServices(IDatabaseProvider databaseProvider, AuthToken agentAuthToken, CancellationToken shutdownCancellationToken) {
		this.TaskManager = new TaskManager(PhantomLogger.Create<TaskManager, ControllerServices>());
		this.MinecraftVersions = new MinecraftVersions();
		
		this.AgentManager = new AgentManager(agentAuthToken, databaseProvider, TaskManager, shutdownCancellationToken);
		this.AgentJavaRuntimesManager = new AgentJavaRuntimesManager();
		this.EventLog = new EventLog(databaseProvider, TaskManager, shutdownCancellationToken);
		this.InstanceManager = new InstanceManager(AgentManager, MinecraftVersions, databaseProvider, shutdownCancellationToken);
		this.InstanceLogManager = new InstanceLogManager();
		
		this.UserManager = new UserManager(databaseProvider);
		this.RoleManager = new RoleManager(databaseProvider);
		this.UserRoleManager = new UserRoleManager(databaseProvider);
		this.PermissionManager = new PermissionManager(databaseProvider);
		
		this.databaseProvider = databaseProvider;
		this.cancellationToken = shutdownCancellationToken;
	}

	public AgentMessageListener CreateAgentMessageListener(RpcClientConnection<IMessageToAgentListener> connection) {
		return new AgentMessageListener(connection, AgentManager, AgentJavaRuntimesManager, InstanceManager, InstanceLogManager, EventLog, cancellationToken);
	}

	public WebMessageListener CreateWebMessageListener(RpcClientConnection<IMessageToWebListener> connection) {
		return new WebMessageListener(connection);
	}

	public async Task Initialize() {
		await DatabaseMigrator.Run(databaseProvider, cancellationToken);
		await PermissionManager.Initialize();
		await RoleManager.Initialize();
		await AgentManager.Initialize();
		await InstanceManager.Initialize();
	}
}
