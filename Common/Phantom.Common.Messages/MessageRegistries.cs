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
		ToAgent.Add<RegisterAgentSuccessMessage, NoReply>(0);
		ToAgent.Add<RegisterAgentFailureMessage, NoReply>(1);
		ToAgent.Add<ConfigureInstanceMessage, InstanceActionResult<ConfigureInstanceResult>>(2);
		ToAgent.Add<LaunchInstanceMessage, InstanceActionResult<LaunchInstanceResult>>(3);
		ToAgent.Add<StopInstanceMessage, InstanceActionResult<StopInstanceResult>>(4);
		ToAgent.Add<SendCommandToInstanceMessage, InstanceActionResult<SendCommandToInstanceResult>>(5);
		ToAgent.Add<ReplyMessage, NoReply>(127);
		
		ToServer.Add<RegisterAgentMessage, NoReply>(0);
		ToServer.Add<UnregisterAgentMessage, NoReply>(1);
		ToServer.Add<AgentIsAliveMessage, NoReply>(2);
		ToServer.Add<AdvertiseJavaRuntimesMessage, NoReply>(3);
		ToServer.Add<ReportInstanceStatusMessage, NoReply>(4);
		ToServer.Add<InstanceOutputMessage, NoReply>(5);
		ToServer.Add<ReplyMessage, NoReply>(127);
	}
}
