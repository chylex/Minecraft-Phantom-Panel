using Phantom.Common.Data;
using Phantom.Common.Data.Replies;
using Phantom.Common.Messages.Agent.BiDirectional;
using Phantom.Common.Messages.Agent.ToAgent;
using Phantom.Common.Messages.Agent.ToController;
using Phantom.Utils.Logging;
using Phantom.Utils.Rpc.Message;

namespace Phantom.Common.Messages.Agent;

public static class AgentMessageRegistries {
	public static MessageRegistry<IMessageToAgent> ToAgent { get; } = new (PhantomLogger.Create("MessageRegistry", nameof(ToAgent)));
	public static MessageRegistry<IMessageToController> ToController { get; } = new (PhantomLogger.Create("MessageRegistry", nameof(ToController)));
	
	public static IMessageDefinitions<IMessageToAgent, IMessageToController, ReplyMessage> Definitions { get; } = new MessageDefinitions();
	
	static AgentMessageRegistries() {
		ToAgent.Add<RegisterAgentSuccessMessage>(0);
		ToAgent.Add<RegisterAgentFailureMessage>(1);
		ToAgent.Add<ConfigureInstanceMessage, Result<ConfigureInstanceResult, InstanceActionFailure>>(2);
		ToAgent.Add<LaunchInstanceMessage, Result<LaunchInstanceResult, InstanceActionFailure>>(3);
		ToAgent.Add<StopInstanceMessage, Result<StopInstanceResult, InstanceActionFailure>>(4);
		ToAgent.Add<SendCommandToInstanceMessage, Result<SendCommandToInstanceResult, InstanceActionFailure>>(5);
		ToAgent.Add<ReplyMessage>(127);
		
		ToController.Add<RegisterAgentMessage>(0);
		ToController.Add<UnregisterAgentMessage>(1);
		ToController.Add<AgentIsAliveMessage>(2);
		ToController.Add<AdvertiseJavaRuntimesMessage>(3);
		ToController.Add<ReportInstanceStatusMessage>(4);
		ToController.Add<InstanceOutputMessage>(5);
		ToController.Add<ReportAgentStatusMessage>(6);
		ToController.Add<ReportInstanceEventMessage>(7);
		ToController.Add<ReportInstancePlayerCountsMessage>(8);
		ToController.Add<ReplyMessage>(127);
	}
	
	private sealed class MessageDefinitions : IMessageDefinitions<IMessageToAgent, IMessageToController, ReplyMessage> {
		public MessageRegistry<IMessageToAgent> ToClient => ToAgent;
		public MessageRegistry<IMessageToController> ToServer => ToController;
		
		public ReplyMessage CreateReplyMessage(uint sequenceId, byte[] serializedReply) {
			return new ReplyMessage(sequenceId, serializedReply);
		}
	}
}
