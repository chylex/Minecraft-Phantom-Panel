using System.Diagnostics.CodeAnalysis;
using NetMQ;
using Phantom.Utils.Logging;
using Phantom.Utils.Rpc.Message;
using Phantom.Utils.Rpc.Sockets;
using Serilog;

namespace Phantom.Utils.Rpc.Runtime;

public abstract class RpcRuntime<TSocket> where TSocket : ThreadSafeSocket {
	private readonly TSocket socket;

	private protected string LoggerName { get; }
	private protected ILogger RuntimeLogger { get; }
	private protected MessageReplyTracker ReplyTracker { get; }
	
	protected RpcRuntime(RpcSocket<TSocket> socket) {
		this.socket = socket.Socket;
		
		this.LoggerName = socket.Config.LoggerName;
		this.RuntimeLogger = PhantomLogger.Create(LoggerName);
		this.ReplyTracker = socket.ReplyTracker;
	}

	protected async Task Launch() {
		[SuppressMessage("ReSharper", "AccessToDisposedClosure")]
		async Task RunTask() {
			try {
				await Run(socket);
			} catch (Exception e) {
				RuntimeLogger.Error(e, "Caught exception in RPC thread.");
			}
		}
		
		try {
			await Task.Factory.StartNew(RunTask, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default).Unwrap();
		} catch (OperationCanceledException) {
			// Ignore.
		} finally {
			await Disconnect(socket);
			
			socket.Dispose();
			RuntimeLogger.Information("ZeroMQ runtime stopped.");
		}
	}
	
	private protected abstract Task Run(TSocket socket);
	
	private protected abstract Task Disconnect(TSocket socket);
}
