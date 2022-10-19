using NetMQ.Sockets;
using Phantom.Common.Logging;
using Phantom.Common.Messages.ToServer;
using Phantom.Utils.Runtime;
using Serilog;

namespace Phantom.Agent.Rpc;

sealed class KeepAliveLoop {
	private static readonly ILogger Logger = PhantomLogger.Create<KeepAliveLoop>();

	private static readonly TimeSpan KeepAliveInterval = TimeSpan.FromSeconds(10);

	private readonly ClientSocket socket;
	private readonly CancellationTokenSource cancellationTokenSource = new ();

	public KeepAliveLoop(ClientSocket socket, TaskManager taskManager) {
		this.socket = socket;
		taskManager.Run(Run);
	}

	private async Task Run() {
		var cancellationToken = cancellationTokenSource.Token;

		Logger.Information("Started keep-alive loop.");
		try {
			while (true) {
				await Task.Delay(KeepAliveInterval, cancellationToken);
				await socket.SendMessage(new AgentIsAliveMessage());
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
