using NetMQ;
using Phantom.Utils.Rpc.Message;
using Phantom.Utils.Tasks;
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
	private readonly ILogger runtimeLogger;
	private readonly MessageReplyTracker replyTracker;
	private readonly TaskManager taskManager;

	protected RpcRuntime(RpcConfiguration configuration, TSocket socket) {
		RpcRuntime.MarkRuntimeCreated();
		RpcRuntime.SetDefaultSocketOptions(socket.Options);
		this.socket = socket;
		this.runtimeLogger = configuration.RuntimeLogger;
		this.replyTracker = new MessageReplyTracker(runtimeLogger);
		this.taskManager = new TaskManager(configuration.TaskManagerLogger);
	}

	protected async Task Launch() {
		Connect(socket);

		void RunTask() {
			try {
				Run(socket, replyTracker, taskManager);
			} catch (Exception e) {
				runtimeLogger.Error(e, "Caught exception in RPC thread.");
			}
		}
		
		try {
			await Task.Factory.StartNew(RunTask, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
		} catch (OperationCanceledException) {
			// ignore
		} finally {
			await taskManager.Stop();
			await Disconnect();
			
			socket.Dispose();
			NetMQConfig.Cleanup();
			runtimeLogger.Information("ZeroMQ client stopped.");
		}
	}
	
	protected abstract void Connect(TSocket socket);
	protected abstract void Run(TSocket socket, MessageReplyTracker replyTracker, TaskManager taskManager);
	
	protected virtual Task Disconnect() {
		return Task.CompletedTask;
	}
}
