using Phantom.Agent.Minecraft.Java;

namespace Phantom.Agent.Services;

public sealed class AgentServices {
	private AgentFolders AgentFolders { get; }
	
	internal JavaRuntimeRepository JavaRuntimeRepository { get; }

	public AgentServices(AgentFolders agentFolders) {
		this.AgentFolders = agentFolders;
		this.JavaRuntimeRepository = new JavaRuntimeRepository();
	}

	public async Task Initialize() {
		await foreach (var runtime in JavaRuntimeDiscovery.Scan(AgentFolders.JavaSearchFolderPath)) {
			JavaRuntimeRepository.Include(runtime);
		}
	}
}
