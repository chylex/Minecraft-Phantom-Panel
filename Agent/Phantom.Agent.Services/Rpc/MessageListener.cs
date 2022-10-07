using NetMQ.Sockets;
using Phantom.Agent.Rpc;
using Phantom.Common.Data.Replies;
using Phantom.Common.Logging;
using Phantom.Common.Messages;
using Phantom.Common.Messages.ToAgent;
using Phantom.Common.Messages.ToServer;
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

	public async Task HandleRegisterAgentSuccessResult(RegisterAgentSuccessMessage message) {
		Logger.Information("Agent authentication successful.");

		foreach (var instanceInfo in message.InitialInstances) {
			if (await agent.InstanceSessionManager.Configure(instanceInfo) != ConfigureInstanceResult.Success) {
				Logger.Fatal("Unable to configure instance \"{Name}\" (GUID {Guid}), shutting down.", instanceInfo.InstanceName, instanceInfo.InstanceGuid);

				shutdownTokenSource.Cancel();
				return;
			}
		}

		await ServerMessaging.SendMessage(new AdvertiseJavaRuntimesMessage(agent.JavaRuntimeRepository.All));
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
	
	public async Task HandleConfigureInstance(ConfigureInstanceMessage message) {
		await socket.SendSimpleReply(message, await agent.InstanceSessionManager.Configure(message.Configuration));
	}
}
