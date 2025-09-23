using Phantom.Common.Data;
using Phantom.Common.Data.Replies;
using Phantom.Common.Messages.Agent.ToAgent;
using Phantom.Common.Messages.Agent.ToController;
using Phantom.Utils.Rpc.Message;

namespace Phantom.Common.Messages.Agent;

public static class AgentMessageRegistries {
	public static MessageRegistry<IMessageToAgent> ToAgent { get; } = new (nameof(ToAgent));
	public static MessageRegistry<IMessageToController> ToController { get; } = new (nameof(ToController));
	
	public static IMessageDefinitions<IMessageToController, IMessageToAgent> Definitions { get; } = new MessageDefinitions();
	
	static AgentMessageRegistries() {
		ToAgent.Add<ConfigureInstanceMessage, Result<ConfigureInstanceResult, InstanceActionFailure>>(1);
		ToAgent.Add<LaunchInstanceMessage, Result<LaunchInstanceResult, InstanceActionFailure>>(2);
		ToAgent.Add<StopInstanceMessage, Result<StopInstanceResult, InstanceActionFailure>>(3);
		ToAgent.Add<SendCommandToInstanceMessage, Result<SendCommandToInstanceResult, InstanceActionFailure>>(4);
		
		ToController.Add<ReportInstanceStatusMessage>(1);
		ToController.Add<InstanceOutputMessage>(2);
		ToController.Add<ReportAgentStatusMessage>(3);
		ToController.Add<ReportInstanceEventMessage>(4);
		ToController.Add<ReportInstancePlayerCountsMessage>(5);
	}
	
	private sealed class MessageDefinitions : IMessageDefinitions<IMessageToController, IMessageToAgent> {
		public MessageRegistry<IMessageToAgent> ToClient => ToAgent;
		public MessageRegistry<IMessageToController> ToServer => ToController;
	}
}
