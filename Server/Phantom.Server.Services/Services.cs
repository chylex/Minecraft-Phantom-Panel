using Phantom.Server.Services.Agents;

namespace Phantom.Server.Services;

public static class Services {
	public static AgentManager AgentManager { get; }

	static Services() {
		var config = ServiceConfiguration.Validate();

		AgentManager = new AgentManager(config.AuthToken);
	}
}
