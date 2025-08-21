using Phantom.Common.Data;
using Phantom.Common.Data.Instance;
using Phantom.Common.Data.Minecraft;
using Phantom.Common.Data.Replies;
using Phantom.Common.Data.Web.Instance;
using Phantom.Common.Messages.Agent;
using Phantom.Common.Messages.Agent.ToAgent;
using Phantom.Controller.Database;
using Phantom.Controller.Services.Agents;
using Phantom.Utils.Actor;

namespace Phantom.Controller.Services.Instances;

sealed class InstanceActor : ReceiveActor<InstanceActor.ICommand> {
	public readonly record struct Init(Instance Instance, ActorRef<AgentActor.ICommand> AgentActor, AgentConnection AgentConnection, IDbContextProvider DbProvider, CancellationToken CancellationToken);
	
	public static Props<ICommand> Factory(Init init) {
		return Props<ICommand>.Create(() => new InstanceActor(init), new ActorConfiguration { SupervisorStrategy = SupervisorStrategies.Resume });
	}
	
	private readonly ActorRef<AgentActor.ICommand> agentActor;
	private readonly AgentConnection agentConnection;
	private readonly CancellationToken cancellationToken;
	
	private readonly Guid instanceGuid;
	
	private InstanceConfiguration configuration;
	private IInstanceStatus status;
	private InstancePlayerCounts? playerCounts;
	private bool launchAutomatically;
	
	private readonly ActorRef<InstanceDatabaseStorageActor.ICommand> databaseStorageActor;
	
	private InstanceActor(Init init) {
		this.agentActor = init.AgentActor;
		this.agentConnection = init.AgentConnection;
		this.cancellationToken = init.CancellationToken;
		
		(this.instanceGuid, this.configuration, this.status, this.playerCounts, this.launchAutomatically) = init.Instance;
		
		this.databaseStorageActor = Context.ActorOf(InstanceDatabaseStorageActor.Factory(new InstanceDatabaseStorageActor.Init(instanceGuid, init.DbProvider, init.CancellationToken)), "DatabaseStorage");
		
		Receive<SetStatusCommand>(SetStatus);
		Receive<SetPlayerCountsCommand>(SetPlayerCounts);
		ReceiveAsyncAndReply<ConfigureInstanceCommand, Result<ConfigureInstanceResult, InstanceActionFailure>>(ConfigureInstance);
		ReceiveAsyncAndReply<LaunchInstanceCommand, Result<LaunchInstanceResult, InstanceActionFailure>>(LaunchInstance);
		ReceiveAsyncAndReply<StopInstanceCommand, Result<StopInstanceResult, InstanceActionFailure>>(StopInstance);
		ReceiveAsyncAndReply<SendCommandToInstanceCommand, Result<SendCommandToInstanceResult, InstanceActionFailure>>(SendMinecraftCommand);
	}
	
	private void NotifyInstanceUpdated() {
		agentActor.Tell(new AgentActor.ReceiveInstanceDataCommand(new Instance(instanceGuid, configuration, status, playerCounts, launchAutomatically)));
	}
	
	private void SetLaunchAutomatically(bool newValue) {
		if (launchAutomatically != newValue) {
			launchAutomatically = newValue;
			NotifyInstanceUpdated();
		}
	}
	
	private async Task<Result<TReply, InstanceActionFailure>> SendInstanceActionMessage<TMessage, TReply>(TMessage message) where TMessage : IMessageToAgent, ICanReply<Result<TReply, InstanceActionFailure>> {
		var reply = await agentConnection.Send<TMessage, Result<TReply, InstanceActionFailure>>(message, TimeSpan.FromSeconds(10), cancellationToken);
		return reply ?? InstanceActionFailure.AgentIsNotResponding;
	}
	
	public interface ICommand {}
	
	public sealed record SetStatusCommand(IInstanceStatus Status) : ICommand;
	
	public sealed record SetPlayerCountsCommand(InstancePlayerCounts? PlayerCounts) : ICommand;
	
	public sealed record ConfigureInstanceCommand(Guid AuditLogUserGuid, Guid InstanceGuid, InstanceConfiguration Configuration, InstanceLaunchProperties LaunchProperties, bool IsCreatingInstance) : ICommand, ICanReply<Result<ConfigureInstanceResult, InstanceActionFailure>>;
	
	public sealed record LaunchInstanceCommand(Guid AuditLogUserGuid) : ICommand, ICanReply<Result<LaunchInstanceResult, InstanceActionFailure>>;
	
	public sealed record StopInstanceCommand(Guid AuditLogUserGuid, MinecraftStopStrategy StopStrategy) : ICommand, ICanReply<Result<StopInstanceResult, InstanceActionFailure>>;
	
	public sealed record SendCommandToInstanceCommand(Guid AuditLogUserGuid, string Command) : ICommand, ICanReply<Result<SendCommandToInstanceResult, InstanceActionFailure>>;
	
	private void SetStatus(SetStatusCommand command) {
		status = command.Status;
		
		if (!status.IsRunning() && status != InstanceStatus.Offline /* Guard against temporary disconnects */) {
			playerCounts = null;
		}
		
		NotifyInstanceUpdated();
	}
	
	private void SetPlayerCounts(SetPlayerCountsCommand command) {
		playerCounts = command.PlayerCounts;
		NotifyInstanceUpdated();
	}
	
	private async Task<Result<ConfigureInstanceResult, InstanceActionFailure>> ConfigureInstance(ConfigureInstanceCommand command) {
		var message = new ConfigureInstanceMessage(command.InstanceGuid, command.Configuration, command.LaunchProperties);
		var result = await SendInstanceActionMessage<ConfigureInstanceMessage, ConfigureInstanceResult>(message);
		
		if (result.Is(ConfigureInstanceResult.Success)) {
			configuration = command.Configuration;
			NotifyInstanceUpdated();
			
			var storeCommand = new InstanceDatabaseStorageActor.StoreInstanceConfigurationCommand(
				command.AuditLogUserGuid,
				command.IsCreatingInstance,
				configuration
			);
			
			databaseStorageActor.Tell(storeCommand);
		}
		
		return result;
	}
	
	private async Task<Result<LaunchInstanceResult, InstanceActionFailure>> LaunchInstance(LaunchInstanceCommand command) {
		var message = new LaunchInstanceMessage(instanceGuid);
		var result = await SendInstanceActionMessage<LaunchInstanceMessage, LaunchInstanceResult>(message);
		
		if (result.Is(LaunchInstanceResult.LaunchInitiated)) {
			SetLaunchAutomatically(true);
			databaseStorageActor.Tell(new InstanceDatabaseStorageActor.StoreInstanceLaunchedCommand(command.AuditLogUserGuid));
		}
		
		return result;
	}
	
	private async Task<Result<StopInstanceResult, InstanceActionFailure>> StopInstance(StopInstanceCommand command) {
		var message = new StopInstanceMessage(instanceGuid, command.StopStrategy);
		var result = await SendInstanceActionMessage<StopInstanceMessage, StopInstanceResult>(message);
		
		if (result.Is(StopInstanceResult.StopInitiated)) {
			SetLaunchAutomatically(false);
			databaseStorageActor.Tell(new InstanceDatabaseStorageActor.StoreInstanceStoppedCommand(command.AuditLogUserGuid, command.StopStrategy));
		}
		
		return result;
	}
	
	private async Task<Result<SendCommandToInstanceResult, InstanceActionFailure>> SendMinecraftCommand(SendCommandToInstanceCommand command) {
		var message = new SendCommandToInstanceMessage(instanceGuid, command.Command);
		var result = await SendInstanceActionMessage<SendCommandToInstanceMessage, SendCommandToInstanceResult>(message);
		
		if (result.Is(SendCommandToInstanceResult.Success)) {
			databaseStorageActor.Tell(new InstanceDatabaseStorageActor.StoreInstanceCommandSentCommand(command.AuditLogUserGuid, command.Command));
		}
		
		return result;
	}
}
