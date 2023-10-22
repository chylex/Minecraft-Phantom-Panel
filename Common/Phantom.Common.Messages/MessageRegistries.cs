using Phantom.Common.Data.Replies;
using Phantom.Common.Logging;
using Phantom.Common.Messages.BiDirectional;
using Phantom.Common.Messages.ToAgent;
using Phantom.Common.Messages.ToServer;
using Phantom.Utils.Rpc.Message;

namespace Phantom.Common.Messages;

public static class MessageRegistries {
	public static MessageRegistry<IMessageToAgentListener> ToAgent { get; } = new (PhantomLogger.Create("MessageRegistry:ToAgent"));
	public static MessageRegistry<IMessageToServerListener> ToServer { get; } = new (PhantomLogger.Create("MessageRegistry:ToServer"));

	static MessageRegistries() {
		ToAgent.Add<RegisterAgentSuccessMessage>(0);
		ToAgent.Add<RegisterAgentFailureMessage>(1);
		ToAgent.Add<ConfigureInstanceMessage, InstanceActionResult<ConfigureInstanceResult>>(2);
		ToAgent.Add<LaunchInstanceMessage, InstanceActionResult<LaunchInstanceResult>>(3);
		ToAgent.Add<StopInstanceMessage, InstanceActionResult<StopInstanceResult>>(4);
		ToAgent.Add<SendCommandToInstanceMessage, InstanceActionResult<SendCommandToInstanceResult>>(5);
		ToAgent.Add<ReplyMessage>(127);
		
		ToServer.Add<RegisterAgentMessage>(0);
		ToServer.Add<UnregisterAgentMessage>(1);
		ToServer.Add<AgentIsAliveMessage>(2);
		ToServer.Add<AdvertiseJavaRuntimesMessage>(3);
		ToServer.Add<ReportInstanceStatusMessage>(4);
		ToServer.Add<InstanceOutputMessage>(5);
		ToServer.Add<ReportAgentStatusMessage>(6);
		ToServer.Add<ReportInstanceEventMessage>(7);
		ToServer.Add<ReplyMessage>(127);
	}
}
