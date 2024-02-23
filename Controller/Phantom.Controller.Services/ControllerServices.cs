using Akka.Actor;
using Phantom.Common.Data;
using Phantom.Common.Messages.Agent;
using Phantom.Common.Messages.Web;
using Phantom.Controller.Database;
using Phantom.Controller.Minecraft;
using Phantom.Controller.Services.Agents;
using Phantom.Controller.Services.Events;
using Phantom.Controller.Services.Instances;
using Phantom.Controller.Services.Rpc;
using Phantom.Controller.Services.Users;
using Phantom.Utils.Actor;
using Phantom.Utils.Logging;
using Phantom.Utils.Rpc.Runtime;
using Phantom.Utils.Tasks;

namespace Phantom.Controller.Services;

public sealed class ControllerServices : IAsyncDisposable {
	private TaskManager TaskManager { get; }
	private ControllerState ControllerState { get; }
	private MinecraftVersions MinecraftVersions { get; }

	private AgentManager AgentManager { get; }
	private InstanceLogManager InstanceLogManager { get; }
	private EventLogManager EventLogManager { get; }

	private UserManager UserManager { get; }
	private RoleManager RoleManager { get; }
	private PermissionManager PermissionManager { get; }

	private UserRoleManager UserRoleManager { get; }
	private UserLoginManager UserLoginManager { get; }
	private AuditLogManager AuditLogManager { get; }
	
	private readonly ActorSystem actorSystem;
	private readonly IDbContextProvider dbProvider;
	private readonly AuthToken webAuthToken;
	private readonly CancellationToken cancellationToken;
	
	public ControllerServices(IDbContextProvider dbProvider, AuthToken agentAuthToken, AuthToken webAuthToken, CancellationToken shutdownCancellationToken) {
		this.dbProvider = dbProvider;
		this.webAuthToken = webAuthToken;
		this.cancellationToken = shutdownCancellationToken;
		
		this.actorSystem = ActorSystemFactory.Create("Controller");

		this.TaskManager = new TaskManager(PhantomLogger.Create<TaskManager, ControllerServices>());
		this.ControllerState = new ControllerState();
		this.MinecraftVersions = new MinecraftVersions();
		
		this.AgentManager = new AgentManager(actorSystem, agentAuthToken, ControllerState, MinecraftVersions, dbProvider, cancellationToken);
		this.InstanceLogManager = new InstanceLogManager();
		
		this.UserManager = new UserManager(dbProvider);
		this.RoleManager = new RoleManager(dbProvider);
		this.PermissionManager = new PermissionManager(dbProvider);

		this.UserRoleManager = new UserRoleManager(dbProvider);
		this.UserLoginManager = new UserLoginManager(UserManager, PermissionManager);
		this.AuditLogManager = new AuditLogManager(dbProvider);
		this.EventLogManager = new EventLogManager(dbProvider, TaskManager, shutdownCancellationToken);
	}

	public AgentMessageListener CreateAgentMessageListener(RpcConnectionToClient<IMessageToAgentListener> connection) {
		return new AgentMessageListener(connection, AgentManager, InstanceLogManager, EventLogManager, cancellationToken);
	}

	public WebMessageListener CreateWebMessageListener(RpcConnectionToClient<IMessageToWebListener> connection) {
		return new WebMessageListener(actorSystem, connection, webAuthToken, ControllerState, UserManager, RoleManager, UserRoleManager, UserLoginManager, AuditLogManager, AgentManager, InstanceLogManager, MinecraftVersions, EventLogManager);
	}

	public async Task Initialize() {
		await DatabaseMigrator.Run(dbProvider, cancellationToken);
		await AgentManager.Initialize();
		await PermissionManager.Initialize();
		await RoleManager.Initialize();
	}

	public async ValueTask DisposeAsync() {
		await actorSystem.Terminate();
		actorSystem.Dispose();
	}
}
