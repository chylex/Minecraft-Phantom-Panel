using Phantom.Common.Messages.ToAgent;

namespace Phantom.Common.Messages;

public interface IMessageToAgentListener {
	Task HandleAgentAuthenticationResult(RegisterAgentResultMessage message);
	Task HandleShutdownAgent(ShutdownAgentMessage message);
	Task HandleCreateInstance(CreateInstanceMessage message);
	Task HandleSetInstanceState(SetInstanceStateMessage message);
	Task HandleSendCommandToInstance(SendCommandToInstanceMessage message);
}
