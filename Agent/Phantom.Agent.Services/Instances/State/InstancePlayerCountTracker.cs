using Phantom.Agent.Minecraft.Instance;
using Phantom.Agent.Minecraft.Server;
using Phantom.Utils.Logging;
using Phantom.Utils.Tasks;
using Phantom.Utils.Threading;

namespace Phantom.Agent.Services.Instances.State;

sealed class InstancePlayerCountTracker : CancellableBackgroundTask {
	private readonly InstanceProcess process;
	private readonly ushort serverPort;

	private readonly TaskCompletionSource firstDetection = AsyncTasks.CreateCompletionSource();
	private readonly ManualResetEventSlim serverOutputEvent = new ();

	private int? onlinePlayerCount;

	public int? OnlinePlayerCount {
		get {
			lock (this) {
				return onlinePlayerCount;
			}
		}
		private set {
			EventHandler<int?>? onlinePlayerCountChanged;
			lock (this) {
				if (onlinePlayerCount == value) {
					return;
				}
				
				onlinePlayerCount = value;
				onlinePlayerCountChanged = OnlinePlayerCountChanged;
			}

			onlinePlayerCountChanged?.Invoke(this, value);
		}
	}

	private event EventHandler<int?>? OnlinePlayerCountChanged;

	private bool isDisposed = false;

	public InstancePlayerCountTracker(InstanceContext context, InstanceProcess process, ushort serverPort) : base(PhantomLogger.Create<InstancePlayerCountTracker>(context.ShortName)) {
		this.process = process;
		this.serverPort = serverPort;
		Start();
	}

	protected override async Task RunTask() {
		// Give the server time to start accepting connections.
		await Task.Delay(TimeSpan.FromSeconds(10), CancellationToken);

		serverOutputEvent.Set();
		process.AddOutputListener(OnOutput, maxLinesToReadFromHistory: 0);
		
		while (!CancellationToken.IsCancellationRequested) {
			serverOutputEvent.Reset();

			OnlinePlayerCount = await TryGetOnlinePlayerCount();
			
			if (!firstDetection.Task.IsCompleted) {
				firstDetection.SetResult();
			}

			await Task.Delay(TimeSpan.FromSeconds(10), CancellationToken);
			await serverOutputEvent.WaitHandle.WaitOneAsync(CancellationToken);
			await Task.Delay(TimeSpan.FromSeconds(1), CancellationToken);
		}
	}

	private async Task<int?> TryGetOnlinePlayerCount() {
		try {
			var (online, maximum) = await ServerStatusProtocol.GetPlayerCounts(serverPort, CancellationToken);
			Logger.Debug("Detected {OnlinePlayerCount} / {MaximumPlayerCount} online player(s).", online, maximum);
			return online;
		} catch (ServerStatusProtocol.ProtocolException e) {
			Logger.Error(e.Message);
			return null;
		} catch (Exception e) {
			Logger.Error(e, "Caught exception while checking online player count.");
			return null;
		}
	}

	public async Task WaitForOnlinePlayers(CancellationToken cancellationToken) {
		await firstDetection.Task.WaitAsync(cancellationToken);

		var onlinePlayersDetected = AsyncTasks.CreateCompletionSource();

		lock (this) {
			if (onlinePlayerCount == null) {
				throw new InvalidOperationException();
			}
			else if (onlinePlayerCount > 0) {
				return;
			}

			OnlinePlayerCountChanged += OnOnlinePlayerCountChanged;

			void OnOnlinePlayerCountChanged(object? sender, int? newPlayerCount) {
				if (newPlayerCount == null) {
					onlinePlayersDetected.TrySetException(new InvalidOperationException());
					OnlinePlayerCountChanged -= OnOnlinePlayerCountChanged;
				}
				else if (newPlayerCount > 0) {
					onlinePlayersDetected.TrySetResult();
					OnlinePlayerCountChanged -= OnOnlinePlayerCountChanged;
				}
			}
		}

		await onlinePlayersDetected.Task;
	}

	private void OnOutput(object? sender, string? line) {
		lock (this) {
			if (!isDisposed) {
				serverOutputEvent.Set();
			}
		}
	}

	protected override void Dispose() {
		lock (this) {
			isDisposed = true;
			onlinePlayerCount = null;
		}

		process.RemoveOutputListener(OnOutput);
		serverOutputEvent.Dispose();
	}
}
