using Phantom.Common.Rpc.Messages.ToAgent;

namespace Phantom.Common.Rpc.Messages;

public interface IMessageToAgentListener {
	Task HandleAgentAuthenticationResult(RegisterAgentResultMessage message);
	Task HandleShutdownAgent(ShutdownAgentMessage message);
	Task HandleCreateInstance(CreateInstanceMessage message);
}
