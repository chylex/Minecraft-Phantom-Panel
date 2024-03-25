using Phantom.Agent.Minecraft.Launcher;
using Phantom.Agent.Services.Backups;
using Phantom.Agent.Services.Instances.Procedures;
using Phantom.Agent.Services.Instances.States;
using Phantom.Common.Data.Backups;
using Phantom.Common.Data.Instance;
using Phantom.Common.Data.Minecraft;
using Phantom.Common.Data.Replies;
using Phantom.Common.Messages.Agent.ToController;
using Phantom.Utils.Actor;
using Phantom.Utils.Actor.Mailbox;
using Phantom.Utils.Logging;
using Serilog;

namespace Phantom.Agent.Services.Instances;

sealed class InstanceActor : ReceiveActor<InstanceActor.ICommand> {
	public readonly record struct Init(Guid InstanceGuid, string ShortName, InstanceServices Services, AgentState AgentState);

	public static Props<ICommand> Factory(Init init) {
		return Props<ICommand>.Create(() => new InstanceActor(init), new ActorConfiguration { SupervisorStrategy = SupervisorStrategies.Resume, MailboxType = UnboundedJumpAheadMailbox.Name });
	}

	private readonly Guid instanceGuid;
	private readonly InstanceServices services;
	private readonly AgentState agentState;
	
	private readonly ILogger logger;
	private readonly InstanceContext context;

	private IInstanceStatus currentStatus = InstanceStatus.NotRunning;
	private IInstanceState currentState = new InstanceNotRunningState();

	private InstanceActor(Init init) {
		this.instanceGuid = init.InstanceGuid;
		this.services = init.Services;
		this.agentState = init.AgentState;
		
		this.logger = PhantomLogger.Create<InstanceActor>(init.ShortName);
		this.context = new InstanceContext(instanceGuid, init.ShortName, logger, services, SelfTyped);

		Receive<ReportInstanceStatusCommand>(ReportInstanceStatus);
		ReceiveAsync<LaunchInstanceCommand>(LaunchInstance);
		ReceiveAsync<StopInstanceCommand>(StopInstance);
		ReceiveAsyncAndReply<SendCommandToInstanceCommand, SendCommandToInstanceResult>(SendCommandToInstance);
		ReceiveAsyncAndReply<BackupInstanceCommand, BackupCreationResult>(BackupInstance);
		Receive<HandleProcessEndedCommand>(HandleProcessEnded);
		ReceiveAsync<ShutdownCommand>(Shutdown);
	}

	private void SetAndReportStatus(IInstanceStatus status) {
		currentStatus = status;
		ReportCurrentStatus();
	}

	private void ReportCurrentStatus() {
		agentState.UpdateInstance(new Instance(instanceGuid, currentStatus));
		services.ControllerConnection.Send(new ReportInstanceStatusMessage(instanceGuid, currentStatus));
	}

	private void TransitionState(IInstanceState newState) {
		if (currentState == newState) {
			return;
		}

		if (currentState is IDisposable disposable) {
			disposable.Dispose();
		}

		logger.Debug("Transitioning instance state to: {NewState}", newState.GetType().Name);
		
		currentState = newState;
		currentState.Initialize();
	}
	
	public interface ICommand {}

	public sealed record ReportInstanceStatusCommand : ICommand;
	
	public sealed record LaunchInstanceCommand(InstanceConfiguration Configuration, IServerLauncher Launcher, InstanceTicketManager.Ticket Ticket, bool IsRestarting, CancellationToken CancellationToken) : ICommand;
	
	public sealed record StopInstanceCommand(MinecraftStopStrategy StopStrategy, CancellationToken CancellationToken) : ICommand;
	
	public sealed record SendCommandToInstanceCommand(string Command, CancellationToken CancellationToken) : ICommand, ICanReply<SendCommandToInstanceResult>;
	
	public sealed record BackupInstanceCommand(BackupManager BackupManager, CancellationToken CancellationToken) : ICommand, ICanReply<BackupCreationResult>;
	
	public sealed record HandleProcessEndedCommand(IInstanceStatus Status) : ICommand, IJumpAhead;
	
	public sealed record ShutdownCommand : ICommand;

	private void ReportInstanceStatus(ReportInstanceStatusCommand command) {
		ReportCurrentStatus();
	}
	
	private async Task LaunchInstance(LaunchInstanceCommand command) {
		if (command.IsRestarting || currentState is not InstanceRunningState) {
			SetAndReportStatus(command.IsRestarting ? InstanceStatus.Restarting : InstanceStatus.Launching);
			TransitionState(await InstanceLaunchProcedure.Run(context, command.Configuration, command.Launcher, command.Ticket, SetAndReportStatus, command.CancellationToken));
		}
	}

	private async Task StopInstance(StopInstanceCommand command) {
		if (currentState is not InstanceRunningState runningState) {
			return;
		}

		IInstanceStatus oldStatus = currentStatus;
		SetAndReportStatus(InstanceStatus.Stopping);
		
		var newState = await InstanceStopProcedure.Run(context, command.StopStrategy, runningState, SetAndReportStatus, command.CancellationToken);
		if (newState is not null) {
			TransitionState(newState);
		}
		else {
			SetAndReportStatus(oldStatus);
		}
	}

	private async Task<SendCommandToInstanceResult> SendCommandToInstance(SendCommandToInstanceCommand command) {
		if (currentState is InstanceRunningState runningState) {
			return await runningState.SendCommand(command.Command, command.CancellationToken);
		}
		else {
			return SendCommandToInstanceResult.InstanceNotRunning;
		}
	}
	
	private async Task<BackupCreationResult> BackupInstance(BackupInstanceCommand command) {
		if (currentState is not InstanceRunningState runningState || runningState.Process.HasEnded) {
			return new BackupCreationResult(BackupCreationResultKind.InstanceNotRunning);
		}
		else {
			return await command.BackupManager.CreateBackup(context.ShortName, runningState.Process, command.CancellationToken);
		}
	}

	private void HandleProcessEnded(HandleProcessEndedCommand command) {
		if (currentState is InstanceRunningState { Process.HasEnded: true }) {
			SetAndReportStatus(command.Status);
			context.ReportEvent(InstanceEvent.Stopped);
			TransitionState(new InstanceNotRunningState());
		}
	}

	private async Task Shutdown(ShutdownCommand command) {
		await StopInstance(new StopInstanceCommand(MinecraftStopStrategy.Instant, CancellationToken.None));
		Context.Stop(Self);
	}
}
