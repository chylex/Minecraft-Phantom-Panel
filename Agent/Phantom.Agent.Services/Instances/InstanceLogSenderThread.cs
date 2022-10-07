using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Phantom.Agent.Rpc;
using Phantom.Common.Logging;
using Phantom.Common.Messages.ToServer;
using Phantom.Utils.Collections;
using Serilog;

namespace Phantom.Agent.Services.Instances; 

sealed class InstanceLogSenderThread {
	private readonly Guid instanceGuid;
	private readonly ILogger logger;
	private readonly CancellationTokenSource cancellationTokenSource;
	private readonly CancellationToken cancellationToken;
	
	private readonly SemaphoreSlim semaphore = new (1, 1);
	private readonly RingBuffer<string> buffer = new (1000);
	
	public InstanceLogSenderThread(Guid instanceGuid, string name) {
		this.instanceGuid = instanceGuid;
		this.logger = PhantomLogger.Create<InstanceLogSenderThread>(name);
		this.cancellationTokenSource = new CancellationTokenSource();
		this.cancellationToken = cancellationTokenSource.Token;
		
		var thread = new Thread(Run) {
			IsBackground = true,
			Name = "Instance Log Sender (" + name + ")"
		};
		
		thread.Start();
	}

	[SuppressMessage("ReSharper", "LocalVariableHidesMember")]
	private async void Run() {
		logger.Verbose("Thread started.");
		
		try {
			while (!cancellationToken.IsCancellationRequested) {
				await semaphore.WaitAsync(cancellationToken);

				ImmutableArray<string> lines;
				
				try {
					lines = buffer.Count > 0 ? buffer.EnumerateLast(uint.MaxValue).ToImmutableArray() : ImmutableArray<string>.Empty;
					buffer.Clear();
				} finally {
					semaphore.Release();
				}

				if (lines.Length > 0) {
					await ServerMessaging.SendMessage(new InstanceOutputMessage(instanceGuid, lines));
				}

				await Task.Delay(TimeSpan.FromMilliseconds(200), cancellationToken);
			}
		} catch (OperationCanceledException) {
			// Ignore.
		} catch (Exception e) {
			logger.Error(e, "Caught exception in thread.");
		} finally {
			cancellationTokenSource.Dispose();
			logger.Verbose("Thread stopped.");
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
		cancellationTokenSource.Cancel();
	}
}
