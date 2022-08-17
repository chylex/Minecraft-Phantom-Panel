using Phantom.Server.Services.Agents;
using Phantom.Server.Services.Instances;

namespace Phantom.Server.Services;

public static class Services {
	public static AgentManager AgentManager { get; }
	public static InstanceManager InstanceManager { get; }

	static Services() {
		var config = ServiceConfiguration.Validate();

		InstanceManager = new InstanceManager();
		AgentManager = new AgentManager(config.AuthToken, InstanceManager);
	}
}
