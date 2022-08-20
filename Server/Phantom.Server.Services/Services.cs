using Phantom.Server.Services.Agents;
using Phantom.Server.Services.Instances;
using Phantom.Server.Services.Rpc;

namespace Phantom.Server.Services;

public static class Services {
	public static AgentManager AgentManager { get; }
	public static InstanceManager InstanceManager { get; }

	internal static MessageReplyTracker MessageReplyTracker { get; }

	static Services() {
		var config = ServiceConfiguration.Validate();

		InstanceManager = new InstanceManager();
		AgentManager = new AgentManager(config.AuthToken, InstanceManager);
		MessageReplyTracker = new MessageReplyTracker();
	}
}
