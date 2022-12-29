using System.Collections.Immutable;
using Phantom.Agent.Rpc;
using Phantom.Common.Logging;
using Phantom.Common.Messages.ToServer;
using Phantom.Utils.Collections;
using Phantom.Utils.Runtime;
using Serilog;

namespace Phantom.Agent.Services.Instances;

sealed class InstanceLogSender {
	private static readonly TimeSpan SendDelay = TimeSpan.FromMilliseconds(200);

	private readonly Guid instanceGuid;
	private readonly ILogger logger;
	private readonly CancellationTokenSource cancellationTokenSource;
	private readonly CancellationToken cancellationToken;

	private readonly SemaphoreSlim semaphore = new (1, 1);
	private readonly RingBuffer<string> buffer = new (1000);

	public InstanceLogSender(TaskManager taskManager, Guid instanceGuid, string name) {
		this.instanceGuid = instanceGuid;
		this.logger = PhantomLogger.Create<InstanceLogSender>(name);
		this.cancellationTokenSource = new CancellationTokenSource();
		this.cancellationToken = cancellationTokenSource.Token;
		taskManager.Run(Run);
	}

	private async Task Run() {
		logger.Verbose("Task started.");

		try {
			try {
				while (!cancellationToken.IsCancellationRequested) {
					await SendOutputToServer(await DequeueOrThrow());
					await Task.Delay(SendDelay, cancellationToken);
				}
			} catch (OperationCanceledException) {
				// Ignore.
			}

			// Flush remaining lines.
			await SendOutputToServer(DequeueWithoutSemaphore());
		} catch (Exception e) {
			logger.Error(e, "Caught exception in task.");
		} finally {
			cancellationTokenSource.Dispose();
			logger.Verbose("Task stopped.");
		}
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
		await semaphore.WaitAsync(cancellationToken);

		try {
			return DequeueWithoutSemaphore();
		} finally {
			semaphore.Release();
		}
	}

	public void Enqueue(string line) {
		try {
			semaphore.Wait(cancellationToken);
		} catch (Exception) {
			return;
		}

		try {
			buffer.Add(line);
		} finally {
			semaphore.Release();
		}
	}

	public void Cancel() {
		try {
			cancellationTokenSource.Cancel();
		} catch (ObjectDisposedException) {
			// Ignore.
		}
	}
}
