using Phantom.Agent.Minecraft.Java;
using Phantom.Agent.Services.Instances;
using Phantom.Common.Data.Agent;

namespace Phantom.Agent.Services;

public sealed class AgentServices {
	private AgentFolders AgentFolders { get; }
	
	internal JavaRuntimeRepository JavaRuntimeRepository { get; }
	internal InstanceSessionManager InstanceSessionManager { get; }

	public AgentServices(AgentInfo agentInfo, AgentFolders agentFolders) {
		this.AgentFolders = agentFolders;
		this.JavaRuntimeRepository = new JavaRuntimeRepository();
		this.InstanceSessionManager = new InstanceSessionManager(agentInfo, agentFolders, JavaRuntimeRepository);
	}

	public async Task Initialize() {
		await foreach (var runtime in JavaRuntimeDiscovery.Scan(AgentFolders.JavaSearchFolderPath)) {
			JavaRuntimeRepository.Include(runtime);
		}
	}

	public async Task Shutdown() {
		await InstanceSessionManager.StopAll();
	}
}
