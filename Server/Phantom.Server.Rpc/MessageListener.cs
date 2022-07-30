using Phantom.Common.Rpc.Messages;
using Phantom.Common.Rpc.Messages.ToServer;

namespace Phantom.Server.Rpc; 

sealed class MessageListener : IMessageToServerListener {
	public void HandleAgentAuthentication(AgentAuthenticationMessage message) {
		message.ToString();
	}
}
