using Phantom.Agent.Rpc;
using Phantom.Common.Data.Instance;
using Phantom.Common.Data.Replies;
using Phantom.Common.Logging;
using Phantom.Common.Messages;
using Phantom.Common.Messages.BiDirectional;
using Phantom.Common.Messages.ToAgent;
using Phantom.Common.Messages.ToServer;
using Phantom.Utils.Rpc.Message;
using Serilog;

namespace Phantom.Agent.Services.Rpc;

public sealed class MessageListener : IMessageToAgentListener {
	private static ILogger Logger { get; } = PhantomLogger.Create<MessageListener>();

	private readonly RpcServerConnection connection;
	private readonly AgentServices agent;
	private readonly CancellationTokenSource shutdownTokenSource;

	public MessageListener(RpcServerConnection connection, AgentServices agent, CancellationTokenSource shutdownTokenSource) {
		this.connection = connection;
		this.agent = agent;
		this.shutdownTokenSource = shutdownTokenSource;
	}

	public async Task<NoReply> HandleRegisterAgentSuccess(RegisterAgentSuccessMessage message) {
		Logger.Information("Agent authentication successful.");

		void ShutdownAfterConfigurationFailed(InstanceConfiguration configuration) {
			Logger.Fatal("Unable to configure instance \"{Name}\" (GUID {Guid}), shutting down.", configuration.InstanceName, configuration.InstanceGuid);
			shutdownTokenSource.Cancel();
		}
		
		foreach (var configureInstanceMessage in message.InitialInstanceConfigurations) {
			var result = await HandleConfigureInstance(configureInstanceMessage);
			if (!result.Is(ConfigureInstanceResult.Success)) {
				ShutdownAfterConfigurationFailed(configureInstanceMessage.Configuration);
				return NoReply.Instance;
			}
		}

		await ServerMessaging.Send(new AdvertiseJavaRuntimesMessage(agent.JavaRuntimeRepository.All));
		await agent.InstanceSessionManager.RefreshAgentStatus();
		
		return NoReply.Instance;
	}

	public Task<NoReply> HandleRegisterAgentFailure(RegisterAgentFailureMessage message) {
		string errorMessage = message.FailureKind switch {
			RegisterAgentFailure.ConnectionAlreadyHasAnAgent => "This connection already has an associated agent.",
			RegisterAgentFailure.InvalidToken                => "Invalid token.",
			_                                                => "Unknown error " + (byte) message.FailureKind + "."
		};

		Logger.Fatal("Agent authentication failed: {Error}", errorMessage);
		Environment.Exit(1);

		return Task.FromResult(NoReply.Instance);
	}
	
	public async Task<InstanceActionResult<ConfigureInstanceResult>> HandleConfigureInstance(ConfigureInstanceMessage message) {
		return await agent.InstanceSessionManager.Configure(message.Configuration, message.LaunchProperties, message.LaunchNow);
	}

	public async Task<InstanceActionResult<LaunchInstanceResult>> HandleLaunchInstance(LaunchInstanceMessage message) {
		return await agent.InstanceSessionManager.Launch(message.InstanceGuid);
	}

	public async Task<InstanceActionResult<StopInstanceResult>> HandleStopInstance(StopInstanceMessage message) {
		return await agent.InstanceSessionManager.Stop(message.InstanceGuid, message.StopStrategy);
	}

	public async Task<InstanceActionResult<SendCommandToInstanceResult>> HandleSendCommandToInstance(SendCommandToInstanceMessage message) {
		return await agent.InstanceSessionManager.SendCommand(message.InstanceGuid, message.Command);
	}

	public Task<NoReply> HandleReply(ReplyMessage message) {
		connection.Receive(message);
		return Task.FromResult(NoReply.Instance);
	}
}
