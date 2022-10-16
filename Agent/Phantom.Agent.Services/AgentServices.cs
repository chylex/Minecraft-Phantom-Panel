using Phantom.Agent.Minecraft.Java;
using Phantom.Agent.Services.Instances;
using Phantom.Common.Data.Agent;
using Phantom.Utils.Runtime;

namespace Phantom.Agent.Services;

public sealed class AgentServices {
	private AgentFolders AgentFolders { get; }
	private TaskManager TaskManager { get; }

	internal JavaRuntimeRepository JavaRuntimeRepository { get; }
	internal InstanceSessionManager InstanceSessionManager { get; }

	public AgentServices(AgentInfo agentInfo, AgentFolders agentFolders, TaskManager taskManager) {
		this.AgentFolders = agentFolders;
		this.TaskManager = taskManager;
		this.JavaRuntimeRepository = new JavaRuntimeRepository();
		this.InstanceSessionManager = new InstanceSessionManager(agentInfo, agentFolders, JavaRuntimeRepository, TaskManager);
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
