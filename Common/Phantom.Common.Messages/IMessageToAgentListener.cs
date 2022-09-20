using Phantom.Common.Messages.ToAgent;

namespace Phantom.Common.Messages;

public interface IMessageToAgentListener {
	Task HandleRegisterAgentSuccessResult(RegisterAgentSuccessMessage message);
	Task HandleRegisterAgentFailureResult(RegisterAgentFailureMessage message);
	Task HandleShutdownAgent(ShutdownAgentMessage message);
	Task HandleConfigureInstance(ConfigureInstanceMessage message);
	Task HandleSetInstanceState(SetInstanceStateMessage message);
	Task HandleSendCommandToInstance(SendCommandToInstanceMessage message);
}
