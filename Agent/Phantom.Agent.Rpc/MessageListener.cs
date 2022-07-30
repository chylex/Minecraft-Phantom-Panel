using NetMQ.Sockets;
using Phantom.Common.Rpc.Messages;
using Phantom.Common.Rpc.Messages.ToAgent;
using Phantom.Utils.Logging;
using Serilog;

namespace Phantom.Agent.Rpc; 

sealed class MessageListener : IMessageToAgentListener {
	private static ILogger Logger { get; } = PhantomLogger.Create<MessageListener>();

	private readonly ClientSocket socket;
	
	public MessageListener(ClientSocket socket) {
		this.socket = socket;
	}

	public Task HandleAgentAuthenticationResult(AgentAuthenticationResultMessage message) {
		if (message.Success) {
			Logger.Information("Agent authentication successful.");
		}
		else {
			Logger.Fatal("Agent authentication failed: {Error}.", message.ErrorMessage);
			Environment.Exit(1);
		}
		
		return Task.CompletedTask;
	}
}
