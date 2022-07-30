using Phantom.Common.Rpc.Messages.ToAgent;

namespace Phantom.Common.Rpc.Messages;

public interface IMessageToAgentListener {
	Task HandleAgentAuthenticationResult(AgentAuthenticationResultMessage message);
}
