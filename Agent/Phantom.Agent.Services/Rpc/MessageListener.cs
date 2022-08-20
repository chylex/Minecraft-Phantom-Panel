using NetMQ.Sockets;
using Phantom.Agent.Rpc;
using Phantom.Common.Data;
using Phantom.Common.Logging;
using Phantom.Common.Messages;
using Phantom.Common.Messages.ToAgent;
using Serilog;

namespace Phantom.Agent.Services.Rpc;

public sealed class MessageListener : IMessageToAgentListener {
	private static ILogger Logger { get; } = PhantomLogger.Create<MessageListener>();

	private readonly ClientSocket socket;
	private readonly AgentServices agent;
	private readonly CancellationTokenSource shutdownTokenSource;

	public MessageListener(ClientSocket socket, AgentServices agent, CancellationTokenSource shutdownTokenSource) {
		this.socket = socket;
		this.agent = agent;
		this.shutdownTokenSource = shutdownTokenSource;
	}

	public Task HandleAgentAuthenticationResult(RegisterAgentResultMessage message) {
		var result = message.Result;
		if (result == RegisterAgentResult.Success) {
			Logger.Information("Agent authentication successful.");
			return Task.CompletedTask;
		}

		string errorMessage = result switch {
			RegisterAgentResult.DuplicateConnection    => "This connection already has an associated agent.",
			RegisterAgentResult.InvalidToken           => "Invalid token.",
			RegisterAgentResult.OldConnectionNotClosed => "The old connection for this agent is still active.",
			_                                          => "Unknown error " + (byte) result + "."
		};

		Logger.Fatal("Agent authentication failed: {Error}.", errorMessage);
		Environment.Exit(1);
		
		return Task.CompletedTask;
	}

	public Task HandleShutdownAgent(ShutdownAgentMessage message) {
		shutdownTokenSource.Cancel();
		return Task.CompletedTask;
	}

	public async Task HandleCreateInstance(CreateInstanceMessage message) {
		var result = agent.InstanceSessionManager.Create(message.Instance);
		await socket.SendSimpleReply(result);
	}

	public Task HandleSetInstanceState(SetInstanceStateMessage message) {
		return Task.CompletedTask;
	}
}
