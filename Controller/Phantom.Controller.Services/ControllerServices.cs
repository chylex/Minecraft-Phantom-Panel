using Akka.Actor;
using Phantom.Controller.Database;
using Phantom.Controller.Minecraft;
using Phantom.Controller.Services.Agents;
using Phantom.Controller.Services.Events;
using Phantom.Controller.Services.Instances;
using Phantom.Controller.Services.Rpc;
using Phantom.Controller.Services.Users;
using Phantom.Controller.Services.Users.Sessions;
using Phantom.Utils.Actor;
using IRpcAgentRegistrar = Phantom.Utils.Rpc.Runtime.Server.IRpcServerClientRegistrar<Phantom.Common.Messages.Agent.IMessageToController, Phantom.Common.Messages.Agent.IMessageToAgent, Phantom.Common.Data.Agent.AgentInfo>;
using IRpcWebRegistrar = Phantom.Utils.Rpc.Runtime.Server.IRpcServerClientRegistrar<Phantom.Common.Messages.Web.IMessageToController, Phantom.Common.Messages.Web.IMessageToWeb, Phantom.Utils.Rpc.Runtime.Server.RpcServerClientHandshake.NoValue>;

namespace Phantom.Controller.Services;

public sealed class ControllerServices : IDisposable {
	public ActorSystem ActorSystem { get; }
	
	private ControllerState ControllerState { get; }
	private MinecraftVersions MinecraftVersions { get; }
	
	private AuthenticatedUserCache AuthenticatedUserCache { get; }
	private UserManager UserManager { get; }
	private RoleManager RoleManager { get; }
	private UserRoleManager UserRoleManager { get; }
	private UserLoginManager UserLoginManager { get; }
	private PermissionManager PermissionManager { get; }
	
	private AgentManager AgentManager { get; }
	private InstanceLogManager InstanceLogManager { get; }
	
	private AuditLogManager AuditLogManager { get; }
	private EventLogManager EventLogManager { get; }
	
	public IRpcAgentRegistrar AgentRegistrar { get; }
	public AgentClientHandshake AgentHandshake { get; }
	public IRpcWebRegistrar WebRegistrar { get; }
	
	private readonly IDbContextProvider dbProvider;
	private readonly CancellationToken cancellationToken;
	
	public ControllerServices(IDbContextProvider dbProvider, CancellationToken shutdownCancellationToken) {
		this.dbProvider = dbProvider;
		this.cancellationToken = shutdownCancellationToken;
		
		this.ActorSystem = ActorSystemFactory.Create("Controller");
		
		this.ControllerState = new ControllerState();
		this.MinecraftVersions = new MinecraftVersions();
		
		this.AuthenticatedUserCache = new AuthenticatedUserCache();
		this.UserManager = new UserManager(AuthenticatedUserCache, ControllerState, dbProvider);
		this.RoleManager = new RoleManager(dbProvider);
		this.UserRoleManager = new UserRoleManager(AuthenticatedUserCache, ControllerState, dbProvider);
		this.UserLoginManager = new UserLoginManager(AuthenticatedUserCache, UserManager, dbProvider);
		this.PermissionManager = new PermissionManager(dbProvider);
		
		this.AgentManager = new AgentManager(ActorSystem, ControllerState, MinecraftVersions, UserLoginManager, dbProvider, cancellationToken);
		this.InstanceLogManager = new InstanceLogManager();
		
		this.AuditLogManager = new AuditLogManager(dbProvider);
		this.EventLogManager = new EventLogManager(ControllerState, ActorSystem, dbProvider, shutdownCancellationToken);
		
		this.AgentRegistrar = new AgentClientRegistrar(ActorSystem, AgentManager, InstanceLogManager, EventLogManager);
		this.AgentHandshake = new AgentClientHandshake(AgentManager);
		this.WebRegistrar = new WebClientRegistrar(ActorSystem, ControllerState, InstanceLogManager, UserManager, RoleManager, UserRoleManager, UserLoginManager, AuditLogManager, AgentManager, MinecraftVersions, EventLogManager);
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
