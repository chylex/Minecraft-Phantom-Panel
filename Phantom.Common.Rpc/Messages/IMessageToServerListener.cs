using Phantom.Common.Rpc.Messages.ToServer;

namespace Phantom.Common.Rpc.Messages; 

public interface IMessageToServerListener {
	void HandleAgentAuthentication(AgentAuthenticationMessage message);
}
