using Akka.Actor;
using Phantom.Common.Data;
using Phantom.Common.Messages.Agent;
using Phantom.Common.Messages.Agent.ToController;
using Phantom.Common.Messages.Web;
using Phantom.Common.Messages.Web.ToController;
using Phantom.Controller.Database;
using Phantom.Controller.Minecraft;
using Phantom.Controller.Services.Agents;
using Phantom.Controller.Services.Events;
using Phantom.Controller.Services.Instances;
using Phantom.Controller.Services.Rpc;
using Phantom.Controller.Services.Users;
using Phantom.Utils.Actor;
using Phantom.Utils.Rpc.Runtime;
using IMessageFromAgentToController = Phantom.Common.Messages.Agent.IMessageToController;
using IMessageFromWebToController = Phantom.Common.Messages.Web.IMessageToController;

namespace Phantom.Controller.Services;

public sealed class ControllerServices : IDisposable {
	public ActorSystem ActorSystem { get; }
	
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

	public IRegistrationHandler<IMessageToAgent, IMessageFromAgentToController, RegisterAgentMessage> AgentRegistrationHandler { get; }
	public IRegistrationHandler<IMessageToWeb, IMessageFromWebToController, RegisterWebMessage> WebRegistrationHandler { get; }

	private readonly IDbContextProvider dbProvider;
	private readonly CancellationToken cancellationToken;

	public ControllerServices(IDbContextProvider dbProvider, AuthToken agentAuthToken, AuthToken webAuthToken, CancellationToken shutdownCancellationToken) {
		this.dbProvider = dbProvider;
		this.cancellationToken = shutdownCancellationToken;
		
		this.ActorSystem = ActorSystemFactory.Create("Controller");

		this.ControllerState = new ControllerState();
		this.MinecraftVersions = new MinecraftVersions();
		
		this.AgentManager = new AgentManager(ActorSystem, agentAuthToken, ControllerState, MinecraftVersions, dbProvider, cancellationToken);
		this.InstanceLogManager = new InstanceLogManager();
		
		this.UserManager = new UserManager(dbProvider);
		this.RoleManager = new RoleManager(dbProvider);
		this.PermissionManager = new PermissionManager(dbProvider);

		this.UserRoleManager = new UserRoleManager(dbProvider);
		this.UserLoginManager = new UserLoginManager(UserManager, PermissionManager);
		this.AuditLogManager = new AuditLogManager(dbProvider);
		this.EventLogManager = new EventLogManager(ActorSystem, dbProvider, shutdownCancellationToken);
		
		this.AgentRegistrationHandler = new AgentRegistrationHandler(AgentManager, InstanceLogManager, EventLogManager);
		this.WebRegistrationHandler = new WebRegistrationHandler(webAuthToken, ControllerState, InstanceLogManager, UserManager, RoleManager, UserRoleManager, UserLoginManager, AuditLogManager, AgentManager, MinecraftVersions, EventLogManager);
	}

	public async Task Initialize() {
		await DatabaseMigrator.Run(dbProvider, cancellationToken);
		await AgentManager.Initialize();
		await PermissionManager.Initialize();
		await RoleManager.Initialize();
	}

	public void Dispose() {
		ActorSystem.Dispose();
	}
}
