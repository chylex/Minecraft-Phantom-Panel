using Akka.Actor;
using Phantom.Common.Messages.Web;
using Phantom.Controller.Minecraft;
using Phantom.Controller.Services.Agents;
using Phantom.Controller.Services.Events;
using Phantom.Controller.Services.Instances;
using Phantom.Controller.Services.Users;
using Phantom.Controller.Services.Users.Sessions;
using Phantom.Utils.Actor;
using Phantom.Utils.Rpc.Message;
using Phantom.Utils.Rpc.Runtime.Server;

namespace Phantom.Controller.Services.Rpc;

sealed class WebClientRegistrar(
	IActorRefFactory actorSystem,
	ControllerState controllerState,
	InstanceLogManager instanceLogManager,
	UserManager userManager,
	RoleManager roleManager,
	UserRoleManager userRoleManager,
	UserLoginManager userLoginManager,
	AuditLogManager auditLogManager,
	AgentManager agentManager,
	MinecraftVersions minecraftVersions,
	EventLogManager eventLogManager
) : IRpcServerClientRegistrar<IMessageToController, IMessageToWeb> {
	public IMessageReceiver<IMessageToController> Register(RpcServerToClientConnection<IMessageToController, IMessageToWeb> connection) {
		var name = "WebClient-" + connection.SessionGuid;
		var init = new WebMessageHandlerActor.Init(connection, controllerState, instanceLogManager, userManager, roleManager, userRoleManager, userLoginManager, auditLogManager, agentManager, minecraftVersions, eventLogManager);
		return new IMessageReceiver<IMessageToController>.Actor(actorSystem.ActorOf(WebMessageHandlerActor.Factory(init), name));
	}
}
