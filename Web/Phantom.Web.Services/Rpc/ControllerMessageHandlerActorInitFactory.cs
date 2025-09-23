using Phantom.Web.Services.Agents;
using Phantom.Web.Services.Authentication;
using Phantom.Web.Services.Instances;

namespace Phantom.Web.Services.Rpc;

public sealed class ControllerMessageHandlerActorInitFactory(
	AgentManager agentManager,
	InstanceManager instanceManager,
	InstanceLogManager instanceLogManager,
	UserSessionRefreshManager userSessionRefreshManager
) {
	public ControllerMessageHandlerActor.Init Create() {
		return new ControllerMessageHandlerActor.Init(agentManager, instanceManager, instanceLogManager, userSessionRefreshManager);
	}
}
