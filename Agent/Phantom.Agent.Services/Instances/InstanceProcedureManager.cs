using Phantom.Agent.Services.Instances.Procedures;
using Phantom.Common.Data.Minecraft;
using Phantom.Utils.Collections;
using Phantom.Utils.Runtime;

namespace Phantom.Agent.Services.Instances;

sealed class InstanceProcedureManager : IAsyncDisposable {
	private readonly record struct CurrentProcedure(IInstanceProcedure Procedure, CancellationTokenSource CancellationTokenSource);

	private readonly ThreadSafeStructRef<CurrentProcedure> currentProcedure = new ();
	private readonly ThreadSafeLinkedList<IInstanceProcedure> procedureQueue = new ();
	private readonly AutoResetEvent procedureQueueReady = new (false);
	private readonly ManualResetEventSlim procedureQueueFinished = new (false);

	private readonly Instance instance;
	private readonly IInstanceContext context;
	private readonly CancellationTokenSource shutdownCancellationTokenSource = new ();

	public InstanceProcedureManager(Instance instance, IInstanceContext context, TaskManager taskManager) {
		this.instance = instance;
		this.context = context;
		taskManager.Run("Procedure manager for instance " + context.ShortName, Run);
	}

	public async Task Enqueue(IInstanceProcedure procedure, bool immediate = false) {
		await procedureQueue.Add(procedure, toFront: immediate, shutdownCancellationTokenSource.Token);
		procedureQueueReady.Set();
	}

	public async Task<IInstanceProcedure?> GetCurrentProcedure(CancellationToken cancellationToken) {
		return (await currentProcedure.Get(cancellationToken))?.Procedure;
	}

	public async Task CancelCurrentProcedure() {
		(await currentProcedure.Get(shutdownCancellationTokenSource.Token))?.CancellationTokenSource.Cancel();
	}

	private async Task Run() {
		try {
			var shutdownCancellationToken = shutdownCancellationTokenSource.Token;
			while (true) {
				await procedureQueueReady.WaitOneAsync(shutdownCancellationToken);
				while (await procedureQueue.TryTakeFromFront(shutdownCancellationToken) is {} nextProcedure) {
					using var procedureCancellationTokenSource = new CancellationTokenSource();
					await currentProcedure.Set(new CurrentProcedure(nextProcedure, procedureCancellationTokenSource), shutdownCancellationToken);
					await RunProcedure(nextProcedure, procedureCancellationTokenSource.Token);
					await currentProcedure.Set(null, shutdownCancellationToken);
				}
			}
		} catch (OperationCanceledException) {
			// Ignore.
		}

		await RunProcedure(new StopInstanceProcedure(MinecraftStopStrategy.Instant), CancellationToken.None);
		procedureQueueFinished.Set();
	}

	private async Task RunProcedure(IInstanceProcedure procedure, CancellationToken cancellationToken) {
		var procedureName = procedure.GetType().Name;

		context.Logger.Debug("Started procedure: {Procedure}", procedureName);
		try {
			var newState = await procedure.Run(context, cancellationToken);
			context.Logger.Debug("Finished procedure: {Procedure}", procedureName);

			if (newState != null) {
				instance.TransitionState(newState);
			}
		} catch (OperationCanceledException) {
			context.Logger.Debug("Cancelled procedure: {Procedure}", procedureName);
		} catch (Exception e) {
			context.Logger.Error(e, "Caught exception while running procedure: {Procedure}", procedureName);
		}
	}

	public async ValueTask DisposeAsync() {
		shutdownCancellationTokenSource.Cancel();

		await CancelCurrentProcedure();
		await procedureQueueFinished.WaitHandle.WaitOneAsync();

		currentProcedure.Dispose();
		procedureQueue.Dispose();
		procedureQueueReady.Dispose();
		procedureQueueFinished.Dispose();
		shutdownCancellationTokenSource.Dispose();
	}
}
