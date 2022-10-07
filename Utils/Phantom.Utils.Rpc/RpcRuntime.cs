using NetMQ;

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

	protected RpcRuntime(TSocket socket, CancellationToken cancellationToken) {
		RpcRuntime.MarkRuntimeCreated();
		RpcRuntime.SetDefaultSocketOptions(socket.Options);
		this.socket = socket;
	}

	protected async Task Launch() {
		Connect(socket);
		
		try {
			await Run(socket);
		} catch (OperationCanceledException) {
			// ignore
		} finally {
			// TODO wait for all tasks started by MessageRegistry.Handle to complete
			await Disconnect(socket);
			socket.Dispose();
			NetMQConfig.Cleanup();
		}
	}
	
	protected abstract void Connect(TSocket socket);
	protected abstract Task Run(TSocket socket);
	
	protected virtual Task Disconnect(TSocket socket) {
		return Task.CompletedTask;
	}
}
