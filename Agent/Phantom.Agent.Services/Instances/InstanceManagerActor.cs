using Phantom.Agent.Minecraft.Java;
using Phantom.Agent.Services.Backups;
using Phantom.Agent.Services.Downloads;
using Phantom.Agent.Services.Instances.Launch;
using Phantom.Agent.Services.Rpc;
using Phantom.Common.Data;
using Phantom.Common.Data.Agent.Instance;
using Phantom.Common.Data.Agent.Instance.Launch;
using Phantom.Common.Data.Instance;
using Phantom.Common.Data.Minecraft;
using Phantom.Common.Data.Replies;
using Phantom.Utils.Actor;
using Phantom.Utils.IO;
using Phantom.Utils.Logging;
using Serilog;

namespace Phantom.Agent.Services.Instances;

sealed class InstanceManagerActor : ReceiveActor<InstanceManagerActor.ICommand> {
	private static readonly ILogger Logger = PhantomLogger.Create<InstanceManagerActor>();
	
	public readonly record struct Init(ControllerConnection ControllerConnection, AgentFolders AgentFolders, AgentState AgentState, JavaRuntimeRepository JavaRuntimeRepository, InstanceTicketManager InstanceTicketManager, BackupManager BackupManager);
	
	public static Props<ICommand> Factory(Init init) {
		return Props<ICommand>.Create(() => new InstanceManagerActor(init), new ActorConfiguration { SupervisorStrategy = SupervisorStrategies.Resume });
	}
	
	private readonly AgentState agentState;
	private readonly AgentFolders agentFolders;
	
	private readonly InstanceServices instanceServices;
	private readonly InstanceTicketManager instanceTicketManager;
	private readonly Dictionary<Guid, Instance> instances = new ();
	
	private readonly CancellationTokenSource shutdownCancellationTokenSource = new ();
	private readonly CancellationToken shutdownCancellationToken;
	
	private uint instanceLoggerSequenceId = 0;
	
	private InstanceManagerActor(Init init) {
		this.agentState = init.AgentState;
		this.agentFolders = init.AgentFolders;
		
		this.instanceServices = new InstanceServices(init.ControllerConnection, init.BackupManager, new FileDownloadManager(), init.JavaRuntimeRepository);
		this.instanceTicketManager = init.InstanceTicketManager;
		this.shutdownCancellationToken = shutdownCancellationTokenSource.Token;
		
		ReceiveAndReply<ConfigureInstanceCommand, Result<ConfigureInstanceResult, InstanceActionFailure>>(ConfigureInstance);
		ReceiveAndReply<LaunchInstanceCommand, Result<LaunchInstanceResult, InstanceActionFailure>>(LaunchInstance);
		ReceiveAndReply<StopInstanceCommand, Result<StopInstanceResult, InstanceActionFailure>>(StopInstance);
		ReceiveAsyncAndReply<SendCommandToInstanceCommand, Result<SendCommandToInstanceResult, InstanceActionFailure>>(SendCommandToInstance);
		ReceiveAsync<ShutdownCommand>(Shutdown);
	}
	
	private sealed record Instance(ActorRef<InstanceActor.ICommand> Actor, InstanceInfo Info, InstanceProperties Properties, InstanceLaunchRecipe? LaunchRecipe);
	
	public interface ICommand;
	
	public sealed record ConfigureInstanceCommand(Guid InstanceGuid, InstanceInfo InstanceInfo, InstanceLaunchRecipe? LaunchRecipe, bool LaunchNow, bool AlwaysReportStatus) : ICommand, ICanReply<Result<ConfigureInstanceResult, InstanceActionFailure>>;
	
	public sealed record LaunchInstanceCommand(Guid InstanceGuid) : ICommand, ICanReply<Result<LaunchInstanceResult, InstanceActionFailure>>;
	
	public sealed record StopInstanceCommand(Guid InstanceGuid, MinecraftStopStrategy StopStrategy) : ICommand, ICanReply<Result<StopInstanceResult, InstanceActionFailure>>;
	
	public sealed record SendCommandToInstanceCommand(Guid InstanceGuid, string Command) : ICommand, ICanReply<Result<SendCommandToInstanceResult, InstanceActionFailure>>;
	
	public sealed record ShutdownCommand : ICommand;
	
	private Result<ConfigureInstanceResult, InstanceActionFailure> ConfigureInstance(ConfigureInstanceCommand command) {
		var instanceGuid = command.InstanceGuid;
		var instanceInfo = command.InstanceInfo;
		var launchRecipe = command.LaunchRecipe;
		
		if (instances.TryGetValue(instanceGuid, out var instance)) {
			instances[instanceGuid] = instance with {
				Info = instanceInfo,
				LaunchRecipe = launchRecipe,
			};
			
			Logger.Information("Reconfigured instance \"{Name}\" (GUID {Guid}).", instanceInfo.InstanceName, instanceGuid);
			
			if (command.AlwaysReportStatus) {
				instance.Actor.Tell(new InstanceActor.ReportInstanceStatusCommand());
			}
		}
		else {
			var instanceLoggerName = PhantomLogger.ShortenGuid(instanceGuid) + "/" + Interlocked.Increment(ref instanceLoggerSequenceId);
			var instanceFolder = Path.Combine(agentFolders.InstancesFolderPath, instanceGuid.ToString());
			var instanceProperties = new InstanceProperties(instanceGuid, instanceFolder);
			var instanceInit = new InstanceActor.Init(agentState, instanceGuid, instanceLoggerName, instanceServices, instanceTicketManager, shutdownCancellationToken);
			instances[instanceGuid] = instance = new Instance(Context.ActorOf(InstanceActor.Factory(instanceInit), "Instance-" + instanceGuid), instanceInfo, instanceProperties, launchRecipe);
			
			Logger.Information("Created instance \"{Name}\" (GUID {Guid}).", instanceInfo.InstanceName, instanceGuid);
			
			instance.Actor.Tell(new InstanceActor.ReportInstanceStatusCommand());
		}
		
		try {
			Directories.Create(instance.Properties.InstanceFolder, Chmod.URWX_GRX);
		} catch (Exception e) {
			Logger.Error(e, "Could not create instance folder: {Path}", instance.Properties.InstanceFolder);
			return ConfigureInstanceResult.CouldNotCreateInstanceFolder;
		}
		
		if (command.LaunchNow) {
			LaunchInstance(new LaunchInstanceCommand(instanceGuid));
		}
		
		return ConfigureInstanceResult.Success;
	}
	
	private Result<LaunchInstanceResult, InstanceActionFailure> LaunchInstance(LaunchInstanceCommand command) {
		var instanceGuid = command.InstanceGuid;
		if (!instances.TryGetValue(instanceGuid, out var instance)) {
			return InstanceActionFailure.InstanceDoesNotExist;
		}
		
		if (instance.LaunchRecipe is not {} launchRecipe) {
			return LaunchInstanceResult.InvalidConfiguration;
		}
		
		var ticket = instanceTicketManager.Reserve(instance.Info);
		if (!ticket) {
			return ticket.Error;
		}
		
		if (agentState.InstancesByGuid.TryGetValue(instanceGuid, out var agentInstance)) {
			var status = agentInstance.Status;
			if (status.IsRunning()) {
				return LaunchInstanceResult.InstanceAlreadyRunning;
			}
			else if (status.IsLaunching()) {
				return LaunchInstanceResult.InstanceAlreadyLaunching;
			}
		}
		
		var pathResolver = new InstancePathResolver(agentFolders, instanceServices.JavaRuntimeRepository, instance.Properties);
		var valueResolver = new InstanceValueResolver(pathResolver);
		var launcher = new InstanceLauncher(instanceServices.DownloadManager, pathResolver, valueResolver, instance.Properties, launchRecipe);
		
		instance.Actor.Tell(new InstanceActor.LaunchInstanceCommand(instance.Info, launcher, ticket.Value, IsRestarting: false));
		
		return LaunchInstanceResult.LaunchInitiated;
	}
	
	private Result<StopInstanceResult, InstanceActionFailure> StopInstance(StopInstanceCommand command) {
		var instanceGuid = command.InstanceGuid;
		if (!instances.TryGetValue(instanceGuid, out var instanceInfo)) {
			return InstanceActionFailure.InstanceDoesNotExist;
		}
		
		if (agentState.InstancesByGuid.TryGetValue(instanceGuid, out var instance)) {
			var status = instance.Status;
			if (status.IsStopping()) {
				return StopInstanceResult.InstanceAlreadyStopping;
			}
			else if (!status.CanStop()) {
				return StopInstanceResult.InstanceAlreadyStopped;
			}
		}
		
		instanceInfo.Actor.Tell(new InstanceActor.StopInstanceCommand(command.StopStrategy));
		return StopInstanceResult.StopInitiated;
	}
	
	private async Task<Result<SendCommandToInstanceResult, InstanceActionFailure>> SendCommandToInstance(SendCommandToInstanceCommand command) {
		var instanceGuid = command.InstanceGuid;
		if (!instances.TryGetValue(instanceGuid, out var instanceInfo)) {
			return InstanceActionFailure.InstanceDoesNotExist;
		}
		
		try {
			return await instanceInfo.Actor.Request(new InstanceActor.SendCommandToInstanceCommand(command.Command), shutdownCancellationToken);
		} catch (OperationCanceledException) {
			return InstanceActionFailure.AgentShuttingDown;
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
