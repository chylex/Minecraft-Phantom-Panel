using Phantom.Common.Data;
using Phantom.Common.Messages.Web;
using Phantom.Common.Messages.Web.ToController;
using Phantom.Common.Messages.Web.ToWeb;
using Phantom.Controller.Minecraft;
using Phantom.Controller.Services.Agents;
using Phantom.Controller.Services.Events;
using Phantom.Controller.Services.Instances;
using Phantom.Controller.Services.Users;
using Phantom.Controller.Services.Users.Sessions;
using Phantom.Utils.Actor;
using Phantom.Utils.Logging;
using Phantom.Utils.Rpc.Runtime;
using Serilog;

namespace Phantom.Controller.Services.Rpc;

sealed class WebRegistrationHandler : IRegistrationHandler<IMessageToWeb, IMessageToController, RegisterWebMessage> {
	private static readonly ILogger Logger = PhantomLogger.Create<WebRegistrationHandler>();
	
	private readonly AuthToken webAuthToken;
	private readonly ControllerState controllerState;
	private readonly InstanceLogManager instanceLogManager;
	private readonly UserManager userManager;
	private readonly RoleManager roleManager;
	private readonly UserRoleManager userRoleManager;
	private readonly UserLoginManager userLoginManager;
	private readonly AuditLogManager auditLogManager;
	private readonly AgentManager agentManager;
	private readonly MinecraftVersions minecraftVersions;
	private readonly EventLogManager eventLogManager;
	
	public WebRegistrationHandler(AuthToken webAuthToken, ControllerState controllerState, InstanceLogManager instanceLogManager, UserManager userManager, RoleManager roleManager, UserRoleManager userRoleManager, UserLoginManager userLoginManager, AuditLogManager auditLogManager, AgentManager agentManager, MinecraftVersions minecraftVersions, EventLogManager eventLogManager) {
		this.webAuthToken = webAuthToken;
		this.controllerState = controllerState;
		this.userManager = userManager;
		this.roleManager = roleManager;
		this.userRoleManager = userRoleManager;
		this.userLoginManager = userLoginManager;
		this.auditLogManager = auditLogManager;
		this.agentManager = agentManager;
		this.minecraftVersions = minecraftVersions;
		this.eventLogManager = eventLogManager;
		this.instanceLogManager = instanceLogManager;
	}
	
	async Task<Props<IMessageToController>?> IRegistrationHandler<IMessageToWeb, IMessageToController, RegisterWebMessage>.TryRegister(RpcConnectionToClient<IMessageToWeb> connection, RegisterWebMessage message) {
		return await TryRegisterImpl(connection, message) ? CreateMessageHandlerActorProps(connection) : null;
	}
	
	public async Task<bool> TryRegisterImpl(RpcConnectionToClient<IMessageToWeb> connection, RegisterWebMessage message) {
		if (webAuthToken.FixedTimeEquals(message.AuthToken)) {
			Logger.Information("Web authorized successfully.");
			await connection.Send(new RegisterWebResultMessage(true));
			return true;
		}
		else {
			Logger.Warning("Web failed to authorize, invalid token.");
			await connection.Send(new RegisterWebResultMessage(false));
			return false;
		}
	}
	
	private Props<IMessageToController> CreateMessageHandlerActorProps(RpcConnectionToClient<IMessageToWeb> connection) {
		var init = new WebMessageHandlerActor.Init(connection, this, controllerState, instanceLogManager, userManager, roleManager, userRoleManager, userLoginManager, auditLogManager, agentManager, minecraftVersions, eventLogManager);
		return WebMessageHandlerActor.Factory(init);
	}
}
