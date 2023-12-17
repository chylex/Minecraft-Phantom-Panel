using Phantom.Common.Data.Instance;
using Phantom.Common.Data.Replies;
using Phantom.Common.Messages.Agent;
using Phantom.Common.Messages.Agent.BiDirectional;
using Phantom.Common.Messages.Agent.ToAgent;
using Phantom.Common.Messages.Agent.ToController;
using Phantom.Utils.Logging;
using Phantom.Utils.Rpc;
using Phantom.Utils.Rpc.Message;
using Serilog;

namespace Phantom.Agent.Services.Rpc;

public sealed class MessageListener : IMessageToAgentListener {
	private static ILogger Logger { get; } = PhantomLogger.Create<MessageListener>();

	private readonly RpcConnectionToServer<IMessageToControllerListener> connection;
	private readonly AgentServices agent;
	private readonly CancellationTokenSource shutdownTokenSource;

	public MessageListener(RpcConnectionToServer<IMessageToControllerListener> connection, AgentServices agent, CancellationTokenSource shutdownTokenSource) {
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
			var result = await HandleConfigureInstance(configureInstanceMessage, alwaysReportStatus: true);
			if (!result.Is(ConfigureInstanceResult.Success)) {
				ShutdownAfterConfigurationFailed(configureInstanceMessage.Configuration);
				return NoReply.Instance;
			}
		}

		await connection.Send(new AdvertiseJavaRuntimesMessage(agent.JavaRuntimeRepository.All));
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
		
		PhantomLogger.Dispose();
		Environment.Exit(1);

		return Task.FromResult(NoReply.Instance);
	}
	
	private Task<InstanceActionResult<ConfigureInstanceResult>> HandleConfigureInstance(ConfigureInstanceMessage message, bool alwaysReportStatus) {
		return agent.InstanceSessionManager.Configure(message.Configuration, message.LaunchProperties, message.LaunchNow, alwaysReportStatus);
	}
	
	public async Task<InstanceActionResult<ConfigureInstanceResult>> HandleConfigureInstance(ConfigureInstanceMessage message) {
		return await HandleConfigureInstance(message, alwaysReportStatus: false);
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
