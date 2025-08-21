using Phantom.Common.Messages.Agent;
using Phantom.Common.Messages.Agent.ToController;
using Phantom.Utils.Logging;
using Phantom.Utils.Rpc.Runtime;
using Serilog;

namespace Phantom.Agent.Rpc;

sealed class KeepAliveLoop {
	private static readonly ILogger Logger = PhantomLogger.Create<KeepAliveLoop>();
	
	private static readonly TimeSpan KeepAliveInterval = TimeSpan.FromSeconds(10);
	
	private readonly RpcConnectionToServer<IMessageToController> connection;
	private readonly CancellationTokenSource cancellationTokenSource = new ();
	
	public KeepAliveLoop(RpcConnectionToServer<IMessageToController> connection) {
		this.connection = connection;
		Task.Run(Run);
	}
	
	private async Task Run() {
		var cancellationToken = cancellationTokenSource.Token;
		
		try {
			await connection.IsReady.WaitAsync(cancellationToken);
			Logger.Information("Started keep-alive loop.");
			
			while (true) {
				await Task.Delay(KeepAliveInterval, cancellationToken);
				await connection.Send(new AgentIsAliveMessage()).WaitAsync(cancellationToken);
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
