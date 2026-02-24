using System.Diagnostics;
using Phantom.Common.Data.Agent.Instance;
using Phantom.Common.Data.Agent.Instance.Stop;
using Phantom.Common.Data.Instance;
using Serilog;

namespace Phantom.Agent.Services.Instances.State;

static class InstanceStopProcedure {
	public static async Task<bool> Run(InstanceContext context, InstanceStopRecipe stopRecipe, InstanceRunningState runningState, Action<IInstanceStatus> reportStatus, CancellationToken cancellationToken) {
		var logger = context.Logger;
		
		var stopCommand = stopRecipe.StopCommand.Resolve(runningState.ValueResolver);
		if (stopCommand == null) {
			logger.Error("Could not resolve stop command");
			return false;
		}
		
		runningState.IsStopping = true;
		
		bool continueStopping = false;
		try {
			var stepExecutor = new StepExecutor(logger, runningState.ValueResolver, runningState.Process, cancellationToken);
			continueStopping = await RunPreparationSteps(context, stopRecipe, stepExecutor);
		} finally {
			if (!continueStopping) {
				runningState.IsStopping = false;
			}
		}
		
		if (!continueStopping) {
			return false;
		}
		
		try {
			// Too late to cancel the stop procedure now.
			runningState.OnStopInitiated();
			
			if (!runningState.Process.HasEnded) {
				logger.Information("Sending stop command...");
				await TrySendStopCommand(context, runningState.Process, stopCommand);
				
				logger.Information("Waiting for session to end...");
				await WaitForSessionToEnd(context, runningState.Process);
			}
		} finally {
			logger.Information("Session stopped.");
			reportStatus(InstanceStatus.NotRunning);
			context.ReportEvent(InstanceEvent.Stopped);
		}
		
		return true;
	}
	
	private static async Task<bool> RunPreparationSteps(InstanceContext context, InstanceStopRecipe stopRecipe, StepExecutor executor) {
		var steps = stopRecipe.Preparation;
		
		for (int stepIndex = 0; stepIndex < steps.Length; stepIndex++) {
			var step = steps[stepIndex];
			try {
				if (await step.Run(executor)) {
					continue;
				}
			} catch (OperationCanceledException) {
				throw;
			} catch (Exception e) {
				context.Logger.Error(e, "Failed preparation step {StepIndex} out of {StepCount}: {StepName}", stepIndex, steps.Length, step.GetType().Name);
			}
			
			return false;
		}
		
		return true;
	}
	
	private sealed class StepExecutor(ILogger logger, IInstanceValueResolver valueResolver, InstanceProcess process, CancellationToken cancellationToken) : IInstanceStopStepExecutor<bool> {
		public async Task<bool> Wait(TimeSpan duration) {
			await Task.Delay(duration, cancellationToken);
			return true;
		}
		
		public async Task<bool> SendToStandardInput(IInstanceValue line) {
			string? command = line.Resolve(valueResolver);
			if (command == null) {
				logger.Error("Could not resolve standard input line: {Value}", line);
				return false;
			}
			
			// If the process can't process standard input, wait a bit but don't block or fail the whole stop procedure.
			await process.TrySendCommand(command, TimeSpan.FromMilliseconds(500), cancellationToken);
			
			return true;
		}
	}
	
	private static async Task TrySendStopCommand(InstanceContext context, InstanceProcess process, string command) {
		using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
		try {
			await process.SendCommand(command, timeout.Token);
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
