using Phantom.Agent.Minecraft.Instance;
using Phantom.Agent.Minecraft.Server;
using Phantom.Agent.Rpc;
using Phantom.Common.Data.Instance;
using Phantom.Common.Messages.Agent.ToController;
using Phantom.Utils.Logging;
using Phantom.Utils.Tasks;
using Phantom.Utils.Threading;

namespace Phantom.Agent.Services.Instances.State;

sealed class InstancePlayerCountTracker : CancellableBackgroundTask {
	private readonly ControllerConnection controllerConnection;
	private readonly Guid instanceGuid;
	private readonly ushort serverPort;
	private readonly InstanceProcess process;

	private readonly TaskCompletionSource firstDetection = AsyncTasks.CreateCompletionSource();
	private readonly ManualResetEventSlim serverOutputEvent = new ();

	private InstancePlayerCounts? playerCounts;

	public InstancePlayerCounts? PlayerCounts {
		get {
			lock (this) {
				return playerCounts;
			}
		}
		private set {
			EventHandler<int?>? onlinePlayerCountChanged;
			lock (this) {
				if (playerCounts == value) {
					return;
				}
				
				playerCounts = value;
				onlinePlayerCountChanged = OnlinePlayerCountChanged;
			}

			onlinePlayerCountChanged?.Invoke(this, value?.Online);
			controllerConnection.Send(new ReportInstancePlayerCountsMessage(instanceGuid, value));
		}
	}

	private event EventHandler<int?>? OnlinePlayerCountChanged;

	private bool isDisposed = false;

	public InstancePlayerCountTracker(InstanceContext context, InstanceProcess process, ushort serverPort) : base(PhantomLogger.Create<InstancePlayerCountTracker>(context.ShortName)) {
		this.controllerConnection = context.Services.ControllerConnection;
		this.instanceGuid = context.InstanceGuid;
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

			PlayerCounts = await TryGetPlayerCounts();
			
			if (!firstDetection.Task.IsCompleted) {
				firstDetection.SetResult();
			}

			await Task.Delay(TimeSpan.FromSeconds(10), CancellationToken);
			await serverOutputEvent.WaitHandle.WaitOneAsync(CancellationToken);
			await Task.Delay(TimeSpan.FromSeconds(1), CancellationToken);
		}
	}

	private async Task<InstancePlayerCounts?> TryGetPlayerCounts() {
		try {
			var result = await ServerStatusProtocol.GetPlayerCounts(serverPort, CancellationToken);
			Logger.Debug("Detected {OnlinePlayerCount} / {MaximumPlayerCount} online player(s).", result.Online, result.Maximum);
			return result;
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
			if (playerCounts is { Online: > 0 }) {
				return;
			}
			else if (playerCounts == null) {
				throw new InvalidOperationException();
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
			playerCounts = null;
		}

		process.RemoveOutputListener(OnOutput);
		serverOutputEvent.Dispose();
	}
}
