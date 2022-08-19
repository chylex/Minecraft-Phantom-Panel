using Phantom.Common.Logging;
using Phantom.Common.Messages.ToAgent;
using Phantom.Common.Messages.ToServer;
using Phantom.Utils.Rpc.Message;

namespace Phantom.Common.Messages; 

public static class MessageRegistries {
	public static MessageRegistry<IMessageToAgentListener, IMessageToAgent> ToAgent { get; } = new (PhantomLogger.Create("MessageRegistry:ToAgent"));
	public static MessageRegistry<IMessageToServerListener, IMessageToServer> ToServer { get; } = new (PhantomLogger.Create("MessageRegistry:ToServer"));

	static MessageRegistries() {
		ToAgent.Add<RegisterAgentResultMessage>(0);
		ToAgent.Add<ShutdownAgentMessage>(1);
		ToAgent.Add<CreateInstanceMessage>(2);
		ToAgent.Add<SetInstanceStateMessage>(3);
		
		ToServer.Add<RegisterAgentMessage>(0);
		ToServer.Add<UnregisterAgentMessage>(1);
	}
}
