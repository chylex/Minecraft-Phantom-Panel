using Phantom.Agent.Minecraft.Java;
using Phantom.Agent.Services.Instances;
using Phantom.Common.Data.Agent;
using Phantom.Common.Logging;
using Phantom.Utils.Runtime;
using Serilog;

namespace Phantom.Agent.Services;

public sealed class AgentServices {
	private static readonly ILogger Logger = PhantomLogger.Create<AgentServices>();
	
	private AgentFolders AgentFolders { get; }
	private TaskManager TaskManager { get; }

	internal JavaRuntimeRepository JavaRuntimeRepository { get; }
	internal InstanceSessionManager InstanceSessionManager { get; }

	public AgentServices(AgentInfo agentInfo, AgentFolders agentFolders) {
		this.AgentFolders = agentFolders;
		this.TaskManager = new TaskManager();
		this.JavaRuntimeRepository = new JavaRuntimeRepository();
		this.InstanceSessionManager = new InstanceSessionManager(agentInfo, agentFolders, JavaRuntimeRepository, TaskManager);
	}

	public async Task Initialize() {
		await foreach (var runtime in JavaRuntimeDiscovery.Scan(AgentFolders.JavaSearchFolderPath)) {
			JavaRuntimeRepository.Include(runtime);
		}
	}

	public async Task Shutdown() {
		Logger.Information("Stopping instances...");
		await InstanceSessionManager.StopAll();
		
		Logger.Information("Stopping task manager...");
		await TaskManager.Stop();
		
		Logger.Information("Services stopped.");
	}
}
