using NetMQ.Sockets;
using Phantom.Agent.Rpc;
using Phantom.Common.Data.Replies;
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

	public Task HandleRegisterAgentSuccessResult(RegisterAgentSuccessMessage message) {
		Logger.Information("Agent authentication successful.");
		
		foreach (var instanceInfo in message.InitialInstances) {
			Logger.Information("Creating initial instance \"{Name}\" (GUID {Guid}).", instanceInfo.InstanceName, instanceInfo.InstanceGuid);
			
			if (agent.InstanceSessionManager.Create(instanceInfo) != CreateInstanceResult.Success) {
				Logger.Fatal("Unable to create instance \"{Name}\" (GUID {Guid}), shutting down.", instanceInfo.InstanceName, instanceInfo.InstanceGuid);
				
				shutdownTokenSource.Cancel();
				return Task.CompletedTask;
			}
		}
		
		return Task.CompletedTask;
	}

	public Task HandleRegisterAgentFailureResult(RegisterAgentFailureMessage message) {
		string errorMessage = message.FailureKind switch {
			RegisterAgentFailure.DuplicateConnection    => "This connection already has an associated agent.",
			RegisterAgentFailure.InvalidToken           => "Invalid token.",
			RegisterAgentFailure.OldConnectionNotClosed => "The old connection for this agent is still active.",
			_                                           => "Unknown error " + (byte) message.FailureKind + "."
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
		await socket.SendSimpleReply(message, agent.InstanceSessionManager.Create(message.Instance));
	}

	public async Task HandleSetInstanceState(SetInstanceStateMessage message) {
		await socket.SendSimpleReply(message, await agent.InstanceSessionManager.Update(message));
	}

	public async Task HandleSendCommandToInstance(SendCommandToInstanceMessage message) {
		await socket.SendSimpleReply(message, await agent.InstanceSessionManager.SendCommand(message));
	}
}
