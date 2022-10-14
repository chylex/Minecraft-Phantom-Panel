using NetMQ;
using Phantom.Utils.Threading;
using Serilog;

namespace Phantom.Utils.Rpc;

static class RpcRuntime {
	private static bool HasRuntime { get; set; }

	internal static void MarkRuntimeCreated() {
		if (HasRuntime) {
			throw new InvalidOperationException("Only one instance of RpcRuntime can be created.");
		}
		
		HasRuntime = true;
	}

	internal static void SetDefaultSocketOptions(ThreadSafeSocketOptions options) {
		// TODO test behavior when either agent or server are offline for a very long time
		options.DelayAttachOnConnect = true;
		options.ReceiveHighWatermark = 10_000;
		options.SendHighWatermark = 10_000;
	}
}

public abstract class RpcRuntime<TSocket> where TSocket : ThreadSafeSocket, new() {
	private readonly TSocket socket;
	private readonly TaskManager taskManager;
	private readonly ILogger logger;

	protected RpcRuntime(TSocket socket, ILogger logger) {
		RpcRuntime.MarkRuntimeCreated();
		RpcRuntime.SetDefaultSocketOptions(socket.Options);
		this.socket = socket;
		this.logger = logger;
		this.taskManager = new TaskManager();
	}

	protected async Task Launch() {
		Connect(socket);
		
		try {
			await Run(socket, taskManager);
		} catch (OperationCanceledException) {
			// ignore
		} finally {
			logger.Information("Stopping task manager...");
			await taskManager.Stop();
			await Disconnect(socket);
			
			socket.Dispose();
			NetMQConfig.Cleanup();
			logger.Information("ZeroMQ client stopped.");
		}
	}
	
	protected abstract void Connect(TSocket socket);
	protected abstract Task Run(TSocket socket, TaskManager taskManager);
	
	protected virtual Task Disconnect(TSocket socket) {
		return Task.CompletedTask;
	}
}
