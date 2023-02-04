using System.Collections.Immutable;
using Phantom.Agent.Rpc;
using Phantom.Common.Logging;
using Phantom.Common.Messages.ToServer;
using Phantom.Utils.Collections;
using Phantom.Utils.Runtime;

namespace Phantom.Agent.Services.Instances;

sealed class InstanceLogSender : CancellableBackgroundTask {
	private static readonly TimeSpan SendDelay = TimeSpan.FromMilliseconds(200);

	private readonly Guid instanceGuid;

	private readonly SemaphoreSlim semaphore = new (1, 1);
	private readonly RingBuffer<string> buffer = new (1000);

	public InstanceLogSender(TaskManager taskManager, Guid instanceGuid, string loggerName) : base(PhantomLogger.Create<InstanceLogSender>(loggerName), taskManager, "Instance log sender for " + loggerName) {
		this.instanceGuid = instanceGuid;
	}
	
	protected override async Task RunTask() {
		try {
			while (!CancellationToken.IsCancellationRequested) {
				await SendOutputToServer(await DequeueOrThrow());
				await Task.Delay(SendDelay, CancellationToken);
			}
		} catch (OperationCanceledException) {
			// Ignore.
		}

		// Flush remaining lines.
		await SendOutputToServer(DequeueWithoutSemaphore());
	}

	private async Task SendOutputToServer(ImmutableArray<string> lines) {
		if (!lines.IsEmpty) {
			await ServerMessaging.Send(new InstanceOutputMessage(instanceGuid, lines));
		}
	}

	private ImmutableArray<string> DequeueWithoutSemaphore() {
		ImmutableArray<string> lines = buffer.Count > 0 ? buffer.EnumerateLast(uint.MaxValue).ToImmutableArray() : ImmutableArray<string>.Empty;
		buffer.Clear();
		return lines;
	}

	private async Task<ImmutableArray<string>> DequeueOrThrow() {
		await semaphore.WaitAsync(CancellationToken);

		try {
			return DequeueWithoutSemaphore();
		} finally {
			semaphore.Release();
		}
	}

	public void Enqueue(string line) {
		try {
			semaphore.Wait(CancellationToken);
		} catch (Exception) {
			return;
		}

		try {
			buffer.Add(line);
		} finally {
			semaphore.Release();
		}
	}
}
