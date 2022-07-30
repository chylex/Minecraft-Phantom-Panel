using NetMQ;

namespace Phantom.Common.Rpc;

static class RpcRuntime {
	private static bool HasRuntime { get; set; }

	internal static void MarkRuntimeCreated() {
		if (HasRuntime) {
			throw new InvalidOperationException("Only one instance of RpcRuntime can be created.");
		}
		
		HasRuntime = true;
	}
}

public abstract class RpcRuntime<TSocket> where TSocket : ThreadSafeSocket, new() {
	private readonly TSocket socket;
	
	protected RpcRuntime() {
		RpcRuntime.MarkRuntimeCreated();
		this.socket = new TSocket();
		this.socket.Options.DelayAttachOnConnect = true;
		this.socket.Options.ReceiveHighWatermark = 10_000;
		this.socket.Options.SendHighWatermark = 10_000;
	}

	protected async Task Launch() {
		Connect(socket);
		
		try {
			await Run(socket);
		} catch (OperationCanceledException) {
			// ignore
		} finally {
			socket.Dispose();
			NetMQConfig.Cleanup();
		}
	}
	
	protected abstract void Connect(TSocket socket);
	protected abstract Task Run(TSocket socket);
}
