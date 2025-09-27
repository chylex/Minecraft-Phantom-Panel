using System.Net.Sockets;
using Phantom.Agent.Minecraft.Instance;
using Phantom.Agent.Minecraft.Server;
using Phantom.Agent.Services.Rpc;
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
	
	private bool WaitingForFirstDetection => !firstDetection.Task.IsCompleted;
	
	private InstancePlayerCounts? playerCounts;
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
		await Task.Delay(TimeSpan.FromSeconds(5), CancellationToken);
		
		serverOutputEvent.Set();
		process.AddOutputListener(OnOutput, maxLinesToReadFromHistory: 0);
		
		while (CancellationToken.Check()) {
			serverOutputEvent.Reset();
			
			InstancePlayerCounts? latestPlayerCounts = await TryGetPlayerCounts();
			UpdatePlayerCounts(latestPlayerCounts);
			
			if (latestPlayerCounts == null) {
				await Task.Delay(WaitingForFirstDetection ? TimeSpan.FromSeconds(5) : TimeSpan.FromSeconds(10), CancellationToken);
			}
			else {
				await Task.Delay(TimeSpan.FromSeconds(10), CancellationToken);
				await serverOutputEvent.WaitHandle.WaitOneAsync(CancellationToken);
				await Task.Delay(TimeSpan.FromSeconds(1), CancellationToken);
			}
		}
	}
	
	private async Task<InstancePlayerCounts?> TryGetPlayerCounts() {
		try {
			return await ServerStatusProtocol.GetPlayerCounts(serverPort, CancellationToken);
		} catch (ServerStatusProtocol.ProtocolException e) {
			Logger.Error("{Message}", e.Message);
			return null;
		} catch (SocketException e) {
			bool waitingForServerStart = e.SocketErrorCode == SocketError.ConnectionRefused && WaitingForFirstDetection;
			if (!waitingForServerStart) {
				Logger.Warning("Could not check online player count. Socket error {ErrorCode} ({ErrorCodeName}), reason: {ErrorMessage}", e.ErrorCode, e.SocketErrorCode, e.Message);
			}
			
			return null;
		} catch (Exception e) {
			Logger.Error(e, "Caught exception while checking online player count.");
			return null;
		}
	}
	
	private void UpdatePlayerCounts(InstancePlayerCounts? newPlayerCounts) {
		if (newPlayerCounts is {} value) {
			Logger.Debug("Detected {OnlinePlayerCount} / {MaximumPlayerCount} online player(s).", value.Online, value.Maximum);
			firstDetection.TrySetResult();
		}
		
		EventHandler<int?>? onlinePlayerCountChanged;
		lock (this) {
			if (playerCounts == newPlayerCounts) {
				return;
			}
			
			playerCounts = newPlayerCounts;
			onlinePlayerCountChanged = OnlinePlayerCountChanged;
		}
		
		onlinePlayerCountChanged?.Invoke(this, newPlayerCounts?.Online);
		
		if (!controllerConnection.TrySend(new ReportInstancePlayerCountsMessage(instanceGuid, newPlayerCounts))) {
			Logger.Warning("Could not report online player count to Controller.");
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
