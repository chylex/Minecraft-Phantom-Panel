using Phantom.Agent.Minecraft.Java;
using Phantom.Agent.Services.Instances;
using Phantom.Agent.Services.Java;
using Phantom.Common.Data.Agent;

namespace Phantom.Agent.Services;

public sealed class AgentServices {
	private AgentInfo AgentInfo { get; }
	private AgentFolders AgentFolders { get; }
	private JavaRuntimeRepository JavaRuntimeRepository { get; }
	
	internal InstanceSessionManager InstanceSessionManager { get; }

	// internal CommandListeners<AgentServices, CommandListener> CommandListeners { get; } = new ();
	// internal CommandQueue<AgentServices, CommandListener> CommandQueue { get; }

	public AgentServices(AgentInfo agentInfo, AgentFolders agentFolders) {
		this.AgentInfo = agentInfo;
		this.AgentFolders = agentFolders;
		this.JavaRuntimeRepository = new JavaRuntimeRepository();
		this.InstanceSessionManager = new InstanceSessionManager(agentInfo, agentFolders, JavaRuntimeRepository);
		// this.CommandQueue = new CommandQueue<AgentServices, CommandListener>(this, CommandListeners, workerCount: 4);
	}

	public async Task Initialize() {
		await foreach (var runtime in JavaRuntimeDiscovery.Scan(AgentFolders.JavaSearchFolderPath)) {
			JavaRuntimeRepository.Include(runtime);
		}
	}

	public async Task Shutdown() {
		// await CommandQueue.Shutdown();
		await InstanceSessionManager.StopAll();
	}
}
