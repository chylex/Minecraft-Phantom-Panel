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
		ToAgent.Add<ShutdownAgentMessage>(2);
		ToAgent.Add<ConfigureInstanceMessage>(3);
		ToAgent.Add<SetInstanceStateMessage>(4);
		ToAgent.Add<SendCommandToInstanceMessage>(5);
		
		ToServer.Add<RegisterAgentMessage>(0);
		ToServer.Add<UnregisterAgentMessage>(1);
		ToServer.Add<AgentIsAliveMessage>(2);
		ToServer.Add<InstanceOutputMessage>(3);
		ToServer.Add<AdvertiseJavaRuntimesMessage>(4);
		ToServer.Add<SimpleReplyMessage>(127);
	}
}
