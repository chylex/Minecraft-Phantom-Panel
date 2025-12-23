using Phantom.Common.Data;
using Phantom.Common.Data.Replies;
using Phantom.Common.Messages.Agent.ToAgent;
using Phantom.Common.Messages.Agent.ToController;
using Phantom.Utils.Rpc.Message;

namespace Phantom.Common.Messages.Agent;

public static class AgentMessageRegistries {
	public static MessageRegistry<IMessageToAgent> ToAgent { get; } = new (nameof(ToAgent));
	public static MessageRegistry<IMessageToController> ToController { get; } = new (nameof(ToController));
	
	public static MessageRegistries<IMessageToController, IMessageToAgent> Registries => new (ToAgent, ToController);
	
	static AgentMessageRegistries() {
		ToAgent.Add<ConfigureInstanceMessage, Result<ConfigureInstanceResult, InstanceActionFailure>>();
		ToAgent.Add<LaunchInstanceMessage, Result<LaunchInstanceResult, InstanceActionFailure>>();
		ToAgent.Add<StopInstanceMessage, Result<StopInstanceResult, InstanceActionFailure>>();
		ToAgent.Add<SendCommandToInstanceMessage, Result<SendCommandToInstanceResult, InstanceActionFailure>>();
		
		ToController.Add<ReportAgentStatusMessage>();
		ToController.Add<ReportInstanceStatusMessage>();
		ToController.Add<ReportInstancePlayerCountsMessage>();
		ToController.Add<ReportInstanceEventMessage>();
		ToController.Add<InstanceOutputMessage>();
	}
}
