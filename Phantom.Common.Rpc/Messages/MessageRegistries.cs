using BinaryPack;
using Phantom.Common.Rpc.Message;
using Phantom.Common.Rpc.Messages.ToServer;

namespace Phantom.Common.Rpc.Messages; 

public static class MessageRegistries {
	public static MessageRegistry<IMessageToAgentListener, IMessageToAgent> ToAgent { get; } = new ();
	public static MessageRegistry<IMessageToServerListener, IMessageToServer> ToServer { get; } = new ();

	static MessageRegistries() {
		ToServer.Add<AgentAuthenticationMessage>(0, BinaryConverter.Deserialize<AgentAuthenticationMessage>);
	}
}
