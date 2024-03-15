using Phantom.Common.Data.Instance;
using Phantom.Common.Data.Replies;
using Phantom.Common.Messages.Agent;
using Phantom.Common.Messages.Agent.BiDirectional;
using Phantom.Common.Messages.Agent.ToAgent;
using Phantom.Common.Messages.Agent.ToController;
using Phantom.Utils.Actor;
using Phantom.Utils.Logging;
using Phantom.Utils.Rpc.Runtime;
using Serilog;

namespace Phantom.Agent.Services.Rpc;

public sealed class ControllerMessageHandlerActor : ReceiveActor<IMessageToAgent> {
	private static ILogger Logger { get; } = PhantomLogger.Create<ControllerMessageHandlerActor>();

	public readonly record struct Init(RpcConnectionToServer<IMessageToController> Connection, AgentServices Agent, CancellationTokenSource ShutdownTokenSource);
	
	public static Props<IMessageToAgent> Factory(Init init) {
		return Props<IMessageToAgent>.Create(() => new ControllerMessageHandlerActor(init), new ActorConfiguration { SupervisorStrategy = SupervisorStrategies.Resume });
	}
	
	private readonly RpcConnectionToServer<IMessageToController> connection;
	private readonly AgentServices agent;
	private readonly CancellationTokenSource shutdownTokenSource;

	private ControllerMessageHandlerActor(Init init) {
		this.connection = init.Connection;
		this.agent = init.Agent;
		this.shutdownTokenSource = init.ShutdownTokenSource;
		
		ReceiveAsync<RegisterAgentSuccessMessage>(HandleRegisterAgentSuccess);
		Receive<RegisterAgentFailureMessage>(HandleRegisterAgentFailure);
		ReceiveAndReplyLater<ConfigureInstanceMessage, InstanceActionResult<ConfigureInstanceResult>>(HandleConfigureInstance);
		ReceiveAndReplyLater<LaunchInstanceMessage, InstanceActionResult<LaunchInstanceResult>>(HandleLaunchInstance);
		ReceiveAndReplyLater<StopInstanceMessage, InstanceActionResult<StopInstanceResult>>(HandleStopInstance);
		ReceiveAndReplyLater<SendCommandToInstanceMessage, InstanceActionResult<SendCommandToInstanceResult>>(HandleSendCommandToInstance);
		Receive<ReplyMessage>(HandleReply);
	}

	private async Task HandleRegisterAgentSuccess(RegisterAgentSuccessMessage message) {
		Logger.Information("Agent authentication successful.");

		void ShutdownAfterConfigurationFailed(Guid instanceGuid, InstanceConfiguration configuration) {
			Logger.Fatal("Unable to configure instance \"{Name}\" (GUID {Guid}), shutting down.", configuration.InstanceName, instanceGuid);
			shutdownTokenSource.Cancel();
		}
		
		foreach (var configureInstanceMessage in message.InitialInstanceConfigurations) {
			var result = await HandleConfigureInstance(configureInstanceMessage, alwaysReportStatus: true);
			if (!result.Is(ConfigureInstanceResult.Success)) {
				ShutdownAfterConfigurationFailed(configureInstanceMessage.InstanceGuid, configureInstanceMessage.Configuration);
				return;
			}
		}

		connection.SetIsReady();
		
		await connection.Send(new AdvertiseJavaRuntimesMessage(agent.JavaRuntimeRepository.All));
		await agent.InstanceSessionManager.RefreshAgentStatus();
	}

	private void HandleRegisterAgentFailure(RegisterAgentFailureMessage message) {
		string errorMessage = message.FailureKind switch {
			RegisterAgentFailure.ConnectionAlreadyHasAnAgent => "This connection already has an associated agent.",
			RegisterAgentFailure.InvalidToken                => "Invalid token.",
			_                                                => "Unknown error " + (byte) message.FailureKind + "."
		};

		Logger.Fatal("Agent authentication failed: {Error}", errorMessage);
		
		PhantomLogger.Dispose();
		Environment.Exit(1);
	}
	
	private Task<InstanceActionResult<ConfigureInstanceResult>> HandleConfigureInstance(ConfigureInstanceMessage message, bool alwaysReportStatus) {
		return agent.InstanceSessionManager.Configure(message.InstanceGuid, message.Configuration, message.LaunchProperties, message.LaunchNow, alwaysReportStatus);
	}
	
	private async Task<InstanceActionResult<ConfigureInstanceResult>> HandleConfigureInstance(ConfigureInstanceMessage message) {
		return await HandleConfigureInstance(message, alwaysReportStatus: false);
	}

	private async Task<InstanceActionResult<LaunchInstanceResult>> HandleLaunchInstance(LaunchInstanceMessage message) {
		return await agent.InstanceSessionManager.Launch(message.InstanceGuid);
	}

	private async Task<InstanceActionResult<StopInstanceResult>> HandleStopInstance(StopInstanceMessage message) {
		return await agent.InstanceSessionManager.Stop(message.InstanceGuid, message.StopStrategy);
	}

	private async Task<InstanceActionResult<SendCommandToInstanceResult>> HandleSendCommandToInstance(SendCommandToInstanceMessage message) {
		return await agent.InstanceSessionManager.SendCommand(message.InstanceGuid, message.Command);
	}

	private void HandleReply(ReplyMessage message) {
		connection.Receive(message);
	}
}
