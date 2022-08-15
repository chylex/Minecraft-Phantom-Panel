using NetMQ.Sockets;
using Phantom.Common.Rpc.Messages;
using Phantom.Common.Rpc.Messages.ToAgent;
using Phantom.Utils.Logging;
using Serilog;

namespace Phantom.Agent; 

sealed class MessageListener : IMessageToAgentListener {
	private static ILogger Logger { get; } = PhantomLogger.Create<MessageListener>();

	private readonly ClientSocket socket;
	private readonly CancellationTokenSource shutdownTokenSource;

	public MessageListener(ClientSocket socket, CancellationTokenSource shutdownTokenSource) {
		this.socket = socket;
		this.shutdownTokenSource = shutdownTokenSource;
	}

	public Task HandleAgentAuthenticationResult(RegisterAgentResultMessage message) {
		if (message.Success) {
			Logger.Information("Agent authentication successful.");
		}
		else {
			Logger.Fatal("Agent authentication failed: {Error}.", message.ErrorMessage);
			Environment.Exit(1);
		}
		
		return Task.CompletedTask;
	}

	public Task HandleShutdownAgent(ShutdownAgentMessage message) {
		
		shutdownTokenSource.Cancel();
		return Task.CompletedTask;
	}
}
