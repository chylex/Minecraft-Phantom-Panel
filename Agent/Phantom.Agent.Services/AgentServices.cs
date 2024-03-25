using Akka.Actor;
using Phantom.Agent.Minecraft.Java;
using Phantom.Agent.Rpc;
using Phantom.Agent.Services.Backups;
using Phantom.Agent.Services.Instances;
using Phantom.Common.Data.Agent;
using Phantom.Utils.Actor;
using Phantom.Utils.Logging;
using Phantom.Utils.Tasks;
using Serilog;

namespace Phantom.Agent.Services;

public sealed class AgentServices {
	private static readonly ILogger Logger = PhantomLogger.Create<AgentServices>();
	
	public ActorSystem ActorSystem { get; }

	private AgentFolders AgentFolders { get; }
	private AgentState AgentState { get; }
	private TaskManager TaskManager { get; }
	private BackupManager BackupManager { get; }

	internal JavaRuntimeRepository JavaRuntimeRepository { get; }
	internal InstanceTicketManager InstanceTicketManager { get; }
	internal ActorRef<InstanceManagerActor.ICommand> InstanceManager { get; }

	public AgentServices(AgentInfo agentInfo, AgentFolders agentFolders, AgentServiceConfiguration serviceConfiguration, ControllerConnection controllerConnection) {
		this.ActorSystem = ActorSystemFactory.Create("Agent");
		
		this.AgentFolders = agentFolders;
		this.AgentState = new AgentState();
		this.TaskManager = new TaskManager(PhantomLogger.Create<TaskManager, AgentServices>());
		this.BackupManager = new BackupManager(agentFolders, serviceConfiguration.MaxConcurrentCompressionTasks);
		
		this.JavaRuntimeRepository = new JavaRuntimeRepository();
		this.InstanceTicketManager = new InstanceTicketManager(agentInfo, controllerConnection);
		
		var instanceManagerInit = new InstanceManagerActor.Init(controllerConnection, agentFolders, AgentState, JavaRuntimeRepository, InstanceTicketManager, TaskManager, BackupManager);
		this.InstanceManager = ActorSystem.ActorOf(InstanceManagerActor.Factory(instanceManagerInit), "InstanceManager");
	}

	public async Task Initialize() {
		await foreach (var runtime in JavaRuntimeDiscovery.Scan(AgentFolders.JavaSearchFolderPath)) {
			JavaRuntimeRepository.Include(runtime);
		}
	}

	public async Task Shutdown() {
		Logger.Information("Stopping services...");
		
		await InstanceManager.Stop(new InstanceManagerActor.ShutdownCommand());
		await TaskManager.Stop();
		
		BackupManager.Dispose();
		
		await ActorSystem.Terminate();
		ActorSystem.Dispose();
		
		Logger.Information("Services stopped.");
	}
}
