using NetMQ;
using Phantom.Utils.Rpc.Message;
using Phantom.Utils.Rpc.Sockets;
using Phantom.Utils.Tasks;
using Serilog;

namespace Phantom.Utils.Rpc;

public abstract class RpcRuntime<TSocket> where TSocket : ThreadSafeSocket {
	private readonly TSocket socket;
	private readonly ILogger runtimeLogger;
	private readonly MessageReplyTracker replyTracker;
	private readonly TaskManager taskManager;

	protected RpcRuntime(RpcSocket<TSocket> socket) {
		this.socket = socket.Socket;
		this.runtimeLogger = socket.Config.RuntimeLogger;
		this.replyTracker = socket.ReplyTracker;
		this.taskManager = new TaskManager(socket.Config.TaskManagerLogger);
	}

	protected async Task Launch() {
		void RunTask() {
			try {
				Run(socket, runtimeLogger, replyTracker, taskManager);
			} catch (Exception e) {
				runtimeLogger.Error(e, "Caught exception in RPC thread.");
			}
		}
		
		try {
			await Task.Factory.StartNew(RunTask, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
		} catch (OperationCanceledException) {
			// Ignore.
		} finally {
			await taskManager.Stop();
			await Disconnect(socket, runtimeLogger);
			
			socket.Dispose();
			runtimeLogger.Information("ZeroMQ runtime stopped.");
		}
	}
	
	protected abstract void Run(TSocket socket, ILogger logger, MessageReplyTracker replyTracker, TaskManager taskManager);
	
	protected virtual Task Disconnect(TSocket socket, ILogger logger) {
		return Task.CompletedTask;
	}
}
