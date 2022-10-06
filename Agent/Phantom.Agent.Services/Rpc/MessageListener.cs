using NetMQ.Sockets;
using Phantom.Common.Data.Replies;
using Phantom.Common.Logging;
using Phantom.Common.Messages;
using Phantom.Common.Messages.ToAgent;
using Serilog;

namespace Phantom.Agent.Services.Rpc;

public sealed class MessageListener : IMessageToAgentListener {
	private static ILogger Logger { get; } = PhantomLogger.Create<MessageListener>();

	private readonly ClientSocket socket;
	private readonly CancellationTokenSource shutdownTokenSource;

	public MessageListener(ClientSocket socket, CancellationTokenSource shutdownTokenSource) {
		this.socket = socket;
		this.shutdownTokenSource = shutdownTokenSource;
	}

	public Task HandleRegisterAgentSuccessResult(RegisterAgentSuccessMessage message) {
		Logger.Information("Agent authentication successful.");
		return Task.CompletedTask;
	}

	public Task HandleRegisterAgentFailureResult(RegisterAgentFailureMessage message) {
		string errorMessage = message.FailureKind switch {
			RegisterAgentFailure.ConnectionAlreadyHasAnAgent => "This connection already has an associated agent.",
			RegisterAgentFailure.InvalidToken                => "Invalid token.",
			_                                                => "Unknown error " + (byte) message.FailureKind + "."
		};

		Logger.Fatal("Agent authentication failed: {Error}", errorMessage);
		Environment.Exit(1);

		return Task.CompletedTask;
	}
}
