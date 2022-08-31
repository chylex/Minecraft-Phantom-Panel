using Phantom.Agent.Services.Instances;
using Phantom.Common.Data.Agent;

namespace Phantom.Agent.Services;

public sealed class AgentServices {
	internal AgentInfo AgentInfo { get; }
	internal InstanceSessionManager InstanceSessionManager { get; }

	// internal CommandListeners<AgentServices, CommandListener> CommandListeners { get; } = new ();
	// internal CommandQueue<AgentServices, CommandListener> CommandQueue { get; }

	public AgentServices(AgentInfo agentInfo, AgentFolders agentFolders) {
		this.AgentInfo = agentInfo;
		this.InstanceSessionManager = new InstanceSessionManager(agentInfo, agentFolders.InstancesFolderPath);
		// this.CommandQueue = new CommandQueue<AgentServices, CommandListener>(this, CommandListeners, workerCount: 4);
	}

	public async Task Shutdown() {
		// await CommandQueue.Shutdown();
		await InstanceSessionManager.StopAll();
	}
}
