using Phantom.Agent.Minecraft.Java;
using Phantom.Agent.Rpc;
using Phantom.Agent.Services.Backups;
using Phantom.Agent.Services.Instances;
using Phantom.Common.Data.Agent;
using Phantom.Utils.Logging;
using Phantom.Utils.Tasks;
using Serilog;

namespace Phantom.Agent.Services;

public sealed class AgentServices {
	private static readonly ILogger Logger = PhantomLogger.Create<AgentServices>();
	
	private AgentFolders AgentFolders { get; }
	private TaskManager TaskManager { get; }
	private BackupManager BackupManager { get; }

	internal JavaRuntimeRepository JavaRuntimeRepository { get; }
	internal InstanceSessionManager InstanceSessionManager { get; }

	public AgentServices(AgentInfo agentInfo, AgentFolders agentFolders, AgentServiceConfiguration serviceConfiguration, ControllerConnection controllerConnection) {
		this.AgentFolders = agentFolders;
		this.TaskManager = new TaskManager(PhantomLogger.Create<TaskManager, AgentServices>());
		this.BackupManager = new BackupManager(agentFolders, serviceConfiguration.MaxConcurrentCompressionTasks);
		this.JavaRuntimeRepository = new JavaRuntimeRepository();
		this.InstanceSessionManager = new InstanceSessionManager(controllerConnection, agentInfo, agentFolders, JavaRuntimeRepository, TaskManager, BackupManager);
	}

	public async Task Initialize() {
		await foreach (var runtime in JavaRuntimeDiscovery.Scan(AgentFolders.JavaSearchFolderPath)) {
			JavaRuntimeRepository.Include(runtime);
		}
	}

	public async Task Shutdown() {
		Logger.Information("Stopping services...");
		
		await InstanceSessionManager.DisposeAsync();
		await TaskManager.Stop();
		
		BackupManager.Dispose();
		
		Logger.Information("Services stopped.");
	}
}
