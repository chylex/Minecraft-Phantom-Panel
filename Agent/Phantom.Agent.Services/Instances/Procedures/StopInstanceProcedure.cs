using System.Diagnostics;
using Phantom.Agent.Minecraft.Command;
using Phantom.Agent.Minecraft.Instance;
using Phantom.Agent.Services.Instances.States;
using Phantom.Common.Data.Instance;
using Phantom.Common.Data.Minecraft;

namespace Phantom.Agent.Services.Instances.Procedures;

sealed record StopInstanceProcedure(MinecraftStopStrategy StopStrategy) : IInstanceProcedure {
	private static readonly ushort[] Stops = { 60, 30, 10, 5, 4, 3, 2, 1, 0 };

	public async Task<IInstanceState?> Run(IInstanceContext context, CancellationToken cancellationToken) {
		if (context.CurrentState is not InstanceRunningState runningState) {
			return null;
		}

		var process = runningState.Process;

		runningState.IsStopping = true;
		context.SetStatus(InstanceStatus.Stopping);

		var seconds = StopStrategy.Seconds;
		if (seconds > 0) {
			try {
				await CountDownWithAnnouncements(context, process, seconds, cancellationToken);
			} catch (OperationCanceledException) {
				runningState.IsStopping = false;
				return null;
			}
		}

		try {
			// Too late to cancel the stop procedure now.
			if (!process.HasEnded) {
				context.Logger.Information("Session stopping now.");
				await DoStop(context, process);
			}
		} finally {
			context.Logger.Information("Session stopped.");
			context.SetStatus(InstanceStatus.NotRunning);
			context.ReportEvent(InstanceEvent.Stopped);
		}

		return new InstanceNotRunningState();
	}

	private async Task CountDownWithAnnouncements(IInstanceContext context, InstanceProcess process, ushort seconds, CancellationToken cancellationToken) {
		context.Logger.Information("Session stopping in {Seconds} seconds.", seconds);

		foreach (var stop in Stops) {
			// TODO change to event-based cancellation
			if (process.HasEnded) {
				return;
			}

			if (seconds > stop) {
				await process.SendCommand(GetCountDownAnnouncementCommand(seconds), cancellationToken);
				await Task.Delay(TimeSpan.FromSeconds(seconds - stop), cancellationToken);
				seconds = stop;
			}
		}
	}

	private static string GetCountDownAnnouncementCommand(ushort seconds) {
		return MinecraftCommand.Say("Server shutting down in " + seconds + (seconds == 1 ? " second." : " seconds."));
	}

	private async Task DoStop(IInstanceContext context, InstanceProcess process) {
		context.Logger.Information("Sending stop command...");
		await TrySendStopCommand(context, process);

		context.Logger.Information("Waiting for session to end...");
		await WaitForSessionToEnd(context, process);
	}

	private async Task TrySendStopCommand(IInstanceContext context, InstanceProcess process) {
		using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
		try {
			await process.SendCommand(MinecraftCommand.Stop, timeout.Token);
		} catch (OperationCanceledException) {
			// Ignore.
		} catch (ObjectDisposedException e) when (e.ObjectName == typeof(Process).FullName && process.HasEnded) {
			// Ignore.
		} catch (Exception e) {
			context.Logger.Warning(e, "Caught exception while sending stop command.");
		}
	}

	private async Task WaitForSessionToEnd(IInstanceContext context, InstanceProcess process) {
		using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(55));
		try {
			await process.WaitForExit(timeout.Token);
		} catch (OperationCanceledException) {
			try {
				context.Logger.Warning("Waiting timed out, killing session...");
				process.Kill();
			} catch (Exception e) {
				context.Logger.Error(e, "Caught exception while killing session.");
			}
		}
	}
}
