using Phantom.Agent.Services.Instances;
using Phantom.Common.Data;
using Phantom.Common.Data.Replies;
using Phantom.Common.Messages.Agent;
using Phantom.Common.Messages.Agent.ToAgent;
using Phantom.Utils.Actor;

namespace Phantom.Agent.Services.Rpc;

public sealed class ControllerMessageHandlerActor : ReceiveActor<IMessageToAgent> {
	public readonly record struct Init(AgentServices Agent);
	
	public static Props<IMessageToAgent> Factory(Init init) {
		return Props<IMessageToAgent>.Create(() => new ControllerMessageHandlerActor(init), new ActorConfiguration { SupervisorStrategy = SupervisorStrategies.Resume });
	}
	
	private readonly AgentServices agent;
	
	private ControllerMessageHandlerActor(Init init) {
		this.agent = init.Agent;
		
		ReceiveAndReplyLater<ConfigureInstanceMessage, Result<ConfigureInstanceResult, InstanceActionFailure>>(HandleConfigureInstance);
		ReceiveAndReplyLater<LaunchInstanceMessage, Result<LaunchInstanceResult, InstanceActionFailure>>(HandleLaunchInstance);
		ReceiveAndReplyLater<StopInstanceMessage, Result<StopInstanceResult, InstanceActionFailure>>(HandleStopInstance);
		ReceiveAndReplyLater<SendCommandToInstanceMessage, Result<SendCommandToInstanceResult, InstanceActionFailure>>(HandleSendCommandToInstance);
	}
	
	private async Task<Result<ConfigureInstanceResult, InstanceActionFailure>> HandleConfigureInstance(ConfigureInstanceMessage message) {
		return await agent.InstanceManager.Request(new InstanceManagerActor.ConfigureInstanceCommand(message.InstanceGuid, message.Configuration, message.LaunchProperties, message.LaunchNow, AlwaysReportStatus: false));
	}
	
	private async Task<Result<LaunchInstanceResult, InstanceActionFailure>> HandleLaunchInstance(LaunchInstanceMessage message) {
		return await agent.InstanceManager.Request(new InstanceManagerActor.LaunchInstanceCommand(message.InstanceGuid));
	}
	
	private async Task<Result<StopInstanceResult, InstanceActionFailure>> HandleStopInstance(StopInstanceMessage message) {
		return await agent.InstanceManager.Request(new InstanceManagerActor.StopInstanceCommand(message.InstanceGuid, message.StopStrategy));
	}
	
	private async Task<Result<SendCommandToInstanceResult, InstanceActionFailure>> HandleSendCommandToInstance(SendCommandToInstanceMessage message) {
		return await agent.InstanceManager.Request(new InstanceManagerActor.SendCommandToInstanceCommand(message.InstanceGuid, message.Command));
	}
}
