using Phantom.Common.Logging;
using Phantom.Common.Messages.ToServer;
using Phantom.Utils.Runtime;
using Serilog;

namespace Phantom.Agent.Rpc;

sealed class KeepAliveLoop {
	private static readonly ILogger Logger = PhantomLogger.Create<KeepAliveLoop>();

	private static readonly TimeSpan KeepAliveInterval = TimeSpan.FromSeconds(10);

	private readonly RpcServerConnection connection;
	private readonly CancellationTokenSource cancellationTokenSource = new ();

	public KeepAliveLoop(RpcServerConnection connection, TaskManager taskManager) {
		this.connection = connection;
		taskManager.Run(Run);
	}

	private async Task Run() {
		var cancellationToken = cancellationTokenSource.Token;

		Logger.Information("Started keep-alive loop.");
		try {
			while (true) {
				await Task.Delay(KeepAliveInterval, cancellationToken);
				await connection.Send(new AgentIsAliveMessage());
			}
		} catch (OperationCanceledException) {
			// Ignore.
		} finally {
			cancellationTokenSource.Dispose();
			Logger.Information("Stopped keep-alive loop.");
		}
	}

	public void Cancel() {
		cancellationTokenSource.Cancel();
	}
}
