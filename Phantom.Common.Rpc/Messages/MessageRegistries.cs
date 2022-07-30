using Phantom.Common.Rpc.Message;
using Phantom.Common.Rpc.Messages.ToAgent;
using Phantom.Common.Rpc.Messages.ToServer;
using Phantom.Utils.Logging;

namespace Phantom.Common.Rpc.Messages; 

public static class MessageRegistries {
	public static MessageRegistry<IMessageToAgentListener, IMessageToAgent> ToAgent { get; } = new (PhantomLogger.Create("MessageRegistry:ToAgent"));
	public static MessageRegistry<IMessageToServerListener, IMessageToServer> ToServer { get; } = new (PhantomLogger.Create("MessageRegistry:ToServer"));

	static MessageRegistries() {
		ToAgent.Add<AgentAuthenticationResultMessage>(0);
		
		ToServer.Add<AgentAuthenticationMessage>(0);
	}
}
