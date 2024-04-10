using System.Diagnostics;
using Phantom.Agent.Minecraft.Command;
using Phantom.Agent.Minecraft.Instance;
using Phantom.Common.Data.Instance;
using Phantom.Common.Data.Minecraft;

namespace Phantom.Agent.Services.Instances.State;

static class InstanceStopProcedure {
	private static readonly ushort[] Stops = { 60, 30, 10, 5, 4, 3, 2, 1, 0 };

	public static async Task<bool> Run(InstanceContext context, MinecraftStopStrategy stopStrategy, InstanceRunningState runningState, Action<IInstanceStatus> reportStatus, CancellationToken cancellationToken) {
		var process = runningState.Process;
		runningState.IsStopping = true;

		var seconds = stopStrategy.Seconds;
		if (seconds > 0) {
			try {
				await CountDownWithAnnouncements(context, process, seconds, cancellationToken);
			} catch (OperationCanceledException) {
				runningState.IsStopping = false;
				return false;
			}
		}

		try {
			// Too late to cancel the stop procedure now.
			runningState.OnStopInitiated();
			
			if (!process.HasEnded) {
				context.Logger.Information("Session stopping now.");
				await DoStop(context, process);
			}
		} finally {
			context.Logger.Information("Session stopped.");
			reportStatus(InstanceStatus.NotRunning);
			context.ReportEvent(InstanceEvent.Stopped);
		}

		return true;
	}

	private static async Task CountDownWithAnnouncements(InstanceContext context, InstanceProcess process, ushort seconds, CancellationToken cancellationToken) {
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

	private static async Task DoStop(InstanceContext context, InstanceProcess process) {
		context.Logger.Information("Sending stop command...");
		await TrySendStopCommand(context, process);

		context.Logger.Information("Waiting for session to end...");
		await WaitForSessionToEnd(context, process);
	}

	private static async Task TrySendStopCommand(InstanceContext context, InstanceProcess process) {
		using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
		try {
			await process.SendCommand(MinecraftCommand.Stop, timeout.Token);
		} catch (OperationCanceledException) {
			// Ignore.
		} catch (ObjectDisposedException e) when (e.ObjectName == typeof(Process).FullName && process.HasEnded) {
			// Ignore.
		} catch (IOException e) when (e.HResult == -2147024664 /* The pipe is being closed */) {
			// Ignore.
		} catch (Exception e) {
			context.Logger.Warning(e, "Caught exception while sending stop command.");
		}
	}

	private static async Task WaitForSessionToEnd(InstanceContext context, InstanceProcess process) {
		try {
			await process.WaitForExit(TimeSpan.FromSeconds(55));
		} catch (TimeoutException) {
			try {
				context.Logger.Warning("Waiting timed out, killing session...");
				process.Kill();
			} catch (Exception e) {
				context.Logger.Error(e, "Caught exception while killing session.");
			}
		}
	}
}
