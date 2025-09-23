using Phantom.Agent.Minecraft.Launcher;
using Phantom.Agent.Services.Backups;
using Phantom.Agent.Services.Instances.State;
using Phantom.Agent.Services.Rpc;
using Phantom.Common.Data.Backups;
using Phantom.Common.Data.Instance;
using Phantom.Common.Data.Minecraft;
using Phantom.Common.Data.Replies;
using Phantom.Common.Messages.Agent.ToController;
using Phantom.Utils.Actor;
using Phantom.Utils.Actor.Mailbox;
using Phantom.Utils.Logging;

namespace Phantom.Agent.Services.Instances;

sealed class InstanceActor : ReceiveActor<InstanceActor.ICommand> {
	public readonly record struct Init(AgentState AgentState, Guid InstanceGuid, string ShortName, InstanceServices InstanceServices, InstanceTicketManager InstanceTicketManager, CancellationToken ShutdownCancellationToken);
	
	public static Props<ICommand> Factory(Init init) {
		return Props<ICommand>.Create(() => new InstanceActor(init), new ActorConfiguration { SupervisorStrategy = SupervisorStrategies.Resume, MailboxType = UnboundedJumpAheadMailbox.Name });
	}
	
	private readonly AgentState agentState;
	private readonly CancellationToken shutdownCancellationToken;
	
	private readonly Guid instanceGuid;
	private readonly InstanceTicketManager instanceTicketManager;
	private readonly InstanceContext context;
	
	private readonly ControllerSendQueue<ReportInstanceStatusMessage> reportStatusQueue;
	private readonly ControllerSendQueue<ReportInstanceEventMessage> reportEventsQueue;
	
	private readonly CancellationTokenSource actorCancellationTokenSource = new ();
	
	private IInstanceStatus currentStatus = InstanceStatus.NotRunning;
	private InstanceRunningState? runningState = null;
	
	private InstanceActor(Init init) {
		InstanceServices services = init.InstanceServices;
		
		this.agentState = init.AgentState;
		this.instanceGuid = init.InstanceGuid;
		this.instanceTicketManager = init.InstanceTicketManager;
		this.shutdownCancellationToken = init.ShutdownCancellationToken;
		
		this.reportStatusQueue = new ControllerSendQueue<ReportInstanceStatusMessage>(services.ControllerConnection, init.ShortName + "-Status", capacity: 1, singleWriter: true);
		this.reportEventsQueue = new ControllerSendQueue<ReportInstanceEventMessage>(services.ControllerConnection, init.ShortName + "-Events", capacity: 1000, singleWriter: true);
		
		var logger = PhantomLogger.Create<InstanceActor>(init.ShortName);
		this.context = new InstanceContext(instanceGuid, init.ShortName, logger, services, reportEventsQueue, SelfTyped, actorCancellationTokenSource.Token);
		
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
		reportStatusQueue.Enqueue(new ReportInstanceStatusMessage(instanceGuid, currentStatus));
	}
	
	private void TransitionState(InstanceRunningState? newState) {
		if (runningState == newState) {
			return;
		}
		
		runningState?.Dispose();
		runningState = newState;
		runningState?.Initialize();
	}
	
	public interface ICommand;
	
	public sealed record ReportInstanceStatusCommand : ICommand;
	
	public sealed record LaunchInstanceCommand(InstanceConfiguration Configuration, IServerLauncher Launcher, InstanceTicketManager.Ticket Ticket, bool IsRestarting) : ICommand;
	
	public sealed record StopInstanceCommand(MinecraftStopStrategy StopStrategy) : ICommand;
	
	public sealed record SendCommandToInstanceCommand(string Command) : ICommand, ICanReply<SendCommandToInstanceResult>;
	
	public sealed record BackupInstanceCommand(BackupManager BackupManager) : ICommand, ICanReply<BackupCreationResult>;
	
	public sealed record HandleProcessEndedCommand(IInstanceStatus Status) : ICommand, IJumpAhead;
	
	public sealed record ShutdownCommand : ICommand;
	
	private void ReportInstanceStatus(ReportInstanceStatusCommand command) {
		ReportCurrentStatus();
	}
	
	private async Task LaunchInstance(LaunchInstanceCommand command) {
		if (command.IsRestarting || runningState is null) {
			SetAndReportStatus(command.IsRestarting ? InstanceStatus.Restarting : InstanceStatus.Launching);
			
			var newState = await InstanceLaunchProcedure.Run(context, command.Configuration, command.Launcher, instanceTicketManager, command.Ticket, SetAndReportStatus, shutdownCancellationToken);
			if (newState is null) {
				instanceTicketManager.Release(command.Ticket);
			}
			
			TransitionState(newState);
		}
	}
	
	private async Task StopInstance(StopInstanceCommand command) {
		if (runningState is null) {
			return;
		}
		
		IInstanceStatus oldStatus = currentStatus;
		SetAndReportStatus(InstanceStatus.Stopping);
		
		if (await InstanceStopProcedure.Run(context, command.StopStrategy, runningState, SetAndReportStatus, shutdownCancellationToken)) {
			instanceTicketManager.Release(runningState.Ticket);
			TransitionState(null);
		}
		else {
			SetAndReportStatus(oldStatus);
		}
	}
	
	private async Task<SendCommandToInstanceResult> SendCommandToInstance(SendCommandToInstanceCommand command) {
		if (runningState is null) {
			return SendCommandToInstanceResult.InstanceNotRunning;
		}
		else {
			return await runningState.SendCommand(command.Command, shutdownCancellationToken);
		}
	}
	
	private async Task<BackupCreationResult> BackupInstance(BackupInstanceCommand command) {
		if (runningState is null || runningState.Process.HasEnded) {
			return new BackupCreationResult(BackupCreationResultKind.InstanceNotRunning);
		}
		else {
			SetAndReportStatus(InstanceStatus.BackingUp);
			try {
				return await command.BackupManager.CreateBackup(context.ShortName, runningState.Process, shutdownCancellationToken);
			} finally {
				SetAndReportStatus(InstanceStatus.Running);
			}
		}
	}
	
	private void HandleProcessEnded(HandleProcessEndedCommand command) {
		if (runningState is { Process.HasEnded: true }) {
			SetAndReportStatus(command.Status);
			context.ReportEvent(InstanceEvent.Stopped);
			instanceTicketManager.Release(runningState.Ticket);
			TransitionState(null);
		}
	}
	
	private async Task Shutdown(ShutdownCommand command) {
		await StopInstance(new StopInstanceCommand(MinecraftStopStrategy.Instant));
		await actorCancellationTokenSource.CancelAsync();
		
		await Task.WhenAll(
			reportStatusQueue.Shutdown(TimeSpan.FromSeconds(5)),
			reportEventsQueue.Shutdown(TimeSpan.FromSeconds(5))
		);
		
		Context.Stop(Self);
	}
}
