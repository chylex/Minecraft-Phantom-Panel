using Akka.Actor;
using Phantom.Agent.Minecraft.Java;
using Phantom.Agent.Services.Backups;
using Phantom.Agent.Services.Instances;
using Phantom.Agent.Services.Rpc;
using Phantom.Common.Data.Agent;
using Phantom.Utils.Actor;
using Phantom.Utils.Logging;
using Serilog;

namespace Phantom.Agent.Services;

public sealed class AgentServices {
	private static readonly ILogger Logger = PhantomLogger.Create<AgentServices>();
	
	public ActorSystem ActorSystem { get; }
	
	private AgentState AgentState { get; }
	private BackupManager BackupManager { get; }
	
	internal JavaRuntimeRepository JavaRuntimeRepository { get; }
	internal InstanceTicketManager InstanceTicketManager { get; }
	internal ActorRef<InstanceManagerActor.ICommand> InstanceManager { get; }
	
	public AgentServices(AgentInfo agentInfo, AgentFolders agentFolders, AgentServiceConfiguration serviceConfiguration, ControllerConnection controllerConnection, JavaRuntimeRepository javaRuntimeRepository) {
		this.ActorSystem = ActorSystemFactory.Create("Agent");
		
		this.AgentState = new AgentState();
		this.BackupManager = new BackupManager(agentFolders, serviceConfiguration.MaxConcurrentCompressionTasks);
		
		this.JavaRuntimeRepository = javaRuntimeRepository;
		this.InstanceTicketManager = new InstanceTicketManager(agentInfo, controllerConnection);
		
		var instanceManagerInit = new InstanceManagerActor.Init(controllerConnection, agentFolders, AgentState, JavaRuntimeRepository, InstanceTicketManager, BackupManager);
		this.InstanceManager = ActorSystem.ActorOf(InstanceManagerActor.Factory(instanceManagerInit), "InstanceManager");
	}
	
	public async Task Shutdown() {
		Logger.Information("Stopping services...");
		
		await InstanceManager.Stop(new InstanceManagerActor.ShutdownCommand());
		await InstanceTicketManager.Shutdown();
		
		BackupManager.Dispose();
		
		await ActorSystem.Terminate();
		ActorSystem.Dispose();
		
		Logger.Information("Services stopped.");
	}
}
