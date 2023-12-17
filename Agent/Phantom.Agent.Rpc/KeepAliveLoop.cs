using Phantom.Common.Messages.Agent;
using Phantom.Common.Messages.Agent.ToController;
using Phantom.Utils.Logging;
using Phantom.Utils.Rpc;
using Serilog;

namespace Phantom.Agent.Rpc;

sealed class KeepAliveLoop {
	private static readonly ILogger Logger = PhantomLogger.Create<KeepAliveLoop>();

	private static readonly TimeSpan KeepAliveInterval = TimeSpan.FromSeconds(10);

	private readonly RpcConnectionToServer<IMessageToControllerListener> connection;
	private readonly CancellationTokenSource cancellationTokenSource = new ();

	public KeepAliveLoop(RpcConnectionToServer<IMessageToControllerListener> connection) {
		this.connection = connection;
		Task.Run(Run);
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
