using Phantom.Agent.Minecraft.Instance;
using Phantom.Agent.Minecraft.Java;
using Phantom.Agent.Minecraft.Launcher;
using Phantom.Agent.Minecraft.Launcher.Types;
using Phantom.Agent.Minecraft.Properties;
using Phantom.Agent.Minecraft.Server;
using Phantom.Agent.Rpc;
using Phantom.Agent.Services.Backups;
using Phantom.Common.Data.Instance;
using Phantom.Common.Data.Minecraft;
using Phantom.Common.Data.Replies;
using Phantom.Utils.Actor;
using Phantom.Utils.IO;
using Phantom.Utils.Logging;
using Phantom.Utils.Tasks;
using Serilog;

namespace Phantom.Agent.Services.Instances;

sealed class InstanceManagerActor : ReceiveActor<InstanceManagerActor.ICommand> {
	private static readonly ILogger Logger = PhantomLogger.Create<InstanceManagerActor>();

	public readonly record struct Init(ControllerConnection ControllerConnection, AgentFolders AgentFolders, AgentState AgentState, JavaRuntimeRepository JavaRuntimeRepository, InstanceTicketManager InstanceTicketManager, BackupManager BackupManager);

	public static Props<ICommand> Factory(Init init) {
		return Props<ICommand>.Create(() => new InstanceManagerActor(init), new ActorConfiguration { SupervisorStrategy = SupervisorStrategies.Resume });
	}
	
	private readonly AgentState agentState;
	private readonly string basePath;

	private readonly InstanceServices instanceServices;
	private readonly InstanceTicketManager instanceTicketManager;
	private readonly Dictionary<Guid, InstanceInfo> instances = new ();

	private readonly CancellationTokenSource shutdownCancellationTokenSource = new ();
	private readonly CancellationToken shutdownCancellationToken;

	private uint instanceLoggerSequenceId = 0;

	private InstanceManagerActor(Init init) {
		this.agentState = init.AgentState;
		this.basePath = init.AgentFolders.InstancesFolderPath;
		this.instanceTicketManager = init.InstanceTicketManager;
		this.shutdownCancellationToken = shutdownCancellationTokenSource.Token;

		var minecraftServerExecutables = new MinecraftServerExecutables(init.AgentFolders.ServerExecutableFolderPath);
		var launchServices = new LaunchServices(minecraftServerExecutables, init.JavaRuntimeRepository);

		this.instanceServices = new InstanceServices(init.ControllerConnection, init.BackupManager, launchServices);
		
		ReceiveAndReply<ConfigureInstanceCommand, InstanceActionResult<ConfigureInstanceResult>>(ConfigureInstance);
		ReceiveAndReply<LaunchInstanceCommand, InstanceActionResult<LaunchInstanceResult>>(LaunchInstance);
		ReceiveAndReply<StopInstanceCommand, InstanceActionResult<StopInstanceResult>>(StopInstance);
		ReceiveAsyncAndReply<SendCommandToInstanceCommand, InstanceActionResult<SendCommandToInstanceResult>>(SendCommandToInstance);
		ReceiveAsync<ShutdownCommand>(Shutdown);
	}

	private string GetInstanceLoggerName(Guid guid) {
		var prefix = guid.ToString();
		return prefix[..prefix.IndexOf('-')] + "/" + Interlocked.Increment(ref instanceLoggerSequenceId);
	}

	private sealed record InstanceInfo(ActorRef<InstanceActor.ICommand> Actor, InstanceConfiguration Configuration, IServerLauncher Launcher);
	
	public interface ICommand {}
	
	public sealed record ConfigureInstanceCommand(Guid InstanceGuid, InstanceConfiguration Configuration, InstanceLaunchProperties LaunchProperties, bool LaunchNow, bool AlwaysReportStatus) : ICommand, ICanReply<InstanceActionResult<ConfigureInstanceResult>>;
	
	public sealed record LaunchInstanceCommand(Guid InstanceGuid) : ICommand, ICanReply<InstanceActionResult<LaunchInstanceResult>>;
	
	public sealed record StopInstanceCommand(Guid InstanceGuid, MinecraftStopStrategy StopStrategy) : ICommand, ICanReply<InstanceActionResult<StopInstanceResult>>;
	
	public sealed record SendCommandToInstanceCommand(Guid InstanceGuid, string Command) : ICommand, ICanReply<InstanceActionResult<SendCommandToInstanceResult>>;
	
	public sealed record ShutdownCommand : ICommand;

	private InstanceActionResult<ConfigureInstanceResult> ConfigureInstance(ConfigureInstanceCommand command) {
		var instanceGuid = command.InstanceGuid;
		var configuration = command.Configuration;

		var instanceFolder = Path.Combine(basePath, instanceGuid.ToString());
		Directories.Create(instanceFolder, Chmod.URWX_GRX);

		var heapMegabytes = configuration.MemoryAllocation.InMegabytes;
		var jvmProperties = new JvmProperties(
			InitialHeapMegabytes: heapMegabytes / 2,
			MaximumHeapMegabytes: heapMegabytes
		);

		var properties = new InstanceProperties(
			instanceGuid,
			configuration.JavaRuntimeGuid,
			jvmProperties,
			configuration.JvmArguments,
			instanceFolder,
			configuration.MinecraftVersion,
			new ServerProperties(configuration.ServerPort, configuration.RconPort),
			command.LaunchProperties
		);

		IServerLauncher launcher = configuration.MinecraftServerKind switch {
			MinecraftServerKind.Vanilla => new VanillaLauncher(properties),
			MinecraftServerKind.Fabric  => new FabricLauncher(properties),
			_                           => InvalidLauncher.Instance
		};

		if (instances.TryGetValue(instanceGuid, out var instance)) {
			instances[instanceGuid] = instance with {
				Configuration = configuration,
				Launcher = launcher
			};
				
			Logger.Information("Reconfigured instance \"{Name}\" (GUID {Guid}).", configuration.InstanceName, instanceGuid);

			if (command.AlwaysReportStatus) {
				instance.Actor.Tell(new InstanceActor.ReportInstanceStatusCommand());
			}
		}
		else {
			var instanceInit = new InstanceActor.Init(agentState, instanceGuid, GetInstanceLoggerName(instanceGuid), instanceServices, instanceTicketManager, shutdownCancellationToken);
			instances[instanceGuid] = instance = new InstanceInfo(Context.ActorOf(InstanceActor.Factory(instanceInit), "Instance-" + instanceGuid), configuration, launcher);
				
			Logger.Information("Created instance \"{Name}\" (GUID {Guid}).", configuration.InstanceName, instanceGuid);

			instance.Actor.Tell(new InstanceActor.ReportInstanceStatusCommand());
		}

		if (command.LaunchNow) {
			LaunchInstance(new LaunchInstanceCommand(instanceGuid));
		}

		return InstanceActionResult.Concrete(ConfigureInstanceResult.Success);
	}

	private InstanceActionResult<LaunchInstanceResult> LaunchInstance(LaunchInstanceCommand command) {
		var instanceGuid = command.InstanceGuid;
		if (!instances.TryGetValue(instanceGuid, out var instanceInfo)) {
			return InstanceActionResult.General<LaunchInstanceResult>(InstanceActionGeneralResult.InstanceDoesNotExist);
		}
		
		var ticket = instanceTicketManager.Reserve(instanceInfo.Configuration);
		if (ticket is Result<InstanceTicketManager.Ticket, LaunchInstanceResult>.Fail fail) {
			return InstanceActionResult.Concrete(fail.Error);
		}

		if (agentState.InstancesByGuid.TryGetValue(instanceGuid, out var instance)) {
			var status = instance.Status;
			if (status.IsRunning()) {
				return InstanceActionResult.Concrete(LaunchInstanceResult.InstanceAlreadyRunning);
			}
			else if (status.IsLaunching()) {
				return InstanceActionResult.Concrete(LaunchInstanceResult.InstanceAlreadyLaunching);
			}
		}
		
		instanceInfo.Actor.Tell(new InstanceActor.LaunchInstanceCommand(instanceInfo.Configuration, instanceInfo.Launcher, ticket.Value, IsRestarting: false));
		return InstanceActionResult.Concrete(LaunchInstanceResult.LaunchInitiated);
	}

	private InstanceActionResult<StopInstanceResult> StopInstance(StopInstanceCommand command) {
		var instanceGuid = command.InstanceGuid;
		if (!instances.TryGetValue(instanceGuid, out var instanceInfo)) {
			return InstanceActionResult.General<StopInstanceResult>(InstanceActionGeneralResult.InstanceDoesNotExist);
		}
        
		if (agentState.InstancesByGuid.TryGetValue(instanceGuid, out var instance)) {
			var status = instance.Status;
			if (status.IsStopping()) {
				return InstanceActionResult.Concrete(StopInstanceResult.InstanceAlreadyStopping);
			}
			else if (!status.CanStop()) {
				return InstanceActionResult.Concrete(StopInstanceResult.InstanceAlreadyStopped);
			}
		}
			
		instanceInfo.Actor.Tell(new InstanceActor.StopInstanceCommand(command.StopStrategy));
		return InstanceActionResult.Concrete(StopInstanceResult.StopInitiated);
	}

	private async Task<InstanceActionResult<SendCommandToInstanceResult>> SendCommandToInstance(SendCommandToInstanceCommand command) {
		var instanceGuid = command.InstanceGuid;
		if (!instances.TryGetValue(instanceGuid, out var instanceInfo)) {
			return InstanceActionResult.General<SendCommandToInstanceResult>(InstanceActionGeneralResult.InstanceDoesNotExist);
		}

		try {
			return InstanceActionResult.Concrete(await instanceInfo.Actor.Request(new InstanceActor.SendCommandToInstanceCommand(command.Command), shutdownCancellationToken));
		} catch (OperationCanceledException) {
			return InstanceActionResult.General<SendCommandToInstanceResult>(InstanceActionGeneralResult.AgentShuttingDown);
		}
	}

	private async Task Shutdown(ShutdownCommand command) {
		Logger.Information("Stopping all instances...");
		
		await shutdownCancellationTokenSource.CancelAsync();
		
		await Task.WhenAll(instances.Values.Select(static instance => instance.Actor.Stop(new InstanceActor.ShutdownCommand())));
		instances.Clear();
		
		shutdownCancellationTokenSource.Dispose();
		
		Logger.Information("All instances stopped.");
		
		Context.Stop(Self);
	}
}
