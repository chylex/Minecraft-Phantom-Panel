using Phantom.Common.Logging;
using Phantom.Common.Messages.ToAgent;
using Phantom.Common.Messages.ToServer;
using Phantom.Utils.Rpc.Message;

namespace Phantom.Common.Messages; 

public static class MessageRegistries {
	public static MessageRegistry<IMessageToAgentListener, IMessageToAgent> ToAgent { get; } = new (PhantomLogger.Create("MessageRegistry:ToAgent"));
	public static MessageRegistry<IMessageToServerListener, IMessageToServer> ToServer { get; } = new (PhantomLogger.Create("MessageRegistry:ToServer"));

	static MessageRegistries() {
		ToAgent.Add<RegisterAgentSuccessMessage>(0);
		ToAgent.Add<RegisterAgentFailureMessage>(1);
		ToAgent.Add<ConfigureInstanceMessage>(2);
		ToAgent.Add<LaunchInstanceMessage>(3);
		ToAgent.Add<StopInstanceMessage>(4);
		
		ToServer.Add<RegisterAgentMessage>(0);
		ToServer.Add<UnregisterAgentMessage>(1);
		ToServer.Add<AgentIsAliveMessage>(2);
		ToServer.Add<AdvertiseJavaRuntimesMessage>(3);
		ToServer.Add<ReportInstanceStatusMessage>(4);
		ToServer.Add<SimpleReplyMessage>(127);
	}
}
