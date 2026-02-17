using Phantom.Agent.Services.Instances.State;
using Phantom.Common.Data;
using Phantom.Common.Data.Agent.Instance;
using Phantom.Common.Data.Instance;

namespace Phantom.Agent.Services.Instances.Launch;

static class InstanceLaunchProcedure {
	public static async Task<InstanceRunningState?> Run(InstanceContext context, InstanceInfo info, InstanceLauncher launcher, InstanceTicketManager ticketManager, InstanceTicketManager.Ticket ticket, Action<IInstanceStatus?> reportStatus, CancellationToken cancellationToken) {
		context.Logger.Information("Session starting...");
		
		Result<InstanceProcess, InstanceLaunchFailReason> result;
		
		if (ticketManager.IsValid(ticket)) {
			try {
				result = await LaunchInstance(context, launcher, reportStatus, cancellationToken);
			} catch (OperationCanceledException) {
				reportStatus(InstanceStatus.NotRunning);
				return null;
			} catch (Exception e) {
				context.Logger.Error(e, "Caught exception while launching instance.");
				result = InstanceLaunchFailReason.UnknownError;
			}
		}
		else {
			context.Logger.Error("Attempted to launch instance with an invalid ticket!");
			result = InstanceLaunchFailReason.UnknownError;
		}
		
		if (result) {
			reportStatus(InstanceStatus.Running);
			context.ReportEvent(InstanceEvent.LaunchSucceeded);
			return new InstanceRunningState(context, info, launcher, ticket, result.Value, cancellationToken);
		}
		else {
			reportStatus(InstanceStatus.Failed(result.Error));
			context.ReportEvent(new InstanceLaunchFailedEvent(result.Error));
			return null;
		}
	}
	
	private static async Task<Result<InstanceProcess, InstanceLaunchFailReason>> LaunchInstance(InstanceContext context, InstanceLauncher launcher, Action<IInstanceStatus?> reportStatus, CancellationToken cancellationToken) {
		cancellationToken.ThrowIfCancellationRequested();
		
		switch (await launcher.Launch(context.Logger, reportStatus, cancellationToken)) {
			case InstanceLaunchResult.Success launchSuccess:
				return launchSuccess.Process;
			
			case InstanceLaunchResult.CouldNotPrepareServerInstance:
				context.Logger.Error("Session failed to launch, could not prepare server instance.");
				return InstanceLaunchFailReason.CouldNotPrepareServerInstance;
			
			case InstanceLaunchResult.CouldNotFindServerExecutable:
				context.Logger.Error("Session failed to launch, could not find server executable.");
				return InstanceLaunchFailReason.CouldNotFindServerExecutable;
			
			case InstanceLaunchResult.CouldNotStartServerExecutable:
				context.Logger.Error("Session failed to launch, could not start server executable.");
				return InstanceLaunchFailReason.CouldNotStartServerExecutable;
			
			default:
				context.Logger.Error("Session failed to launch.");
				return InstanceLaunchFailReason.UnknownError;
		}
	}
}
