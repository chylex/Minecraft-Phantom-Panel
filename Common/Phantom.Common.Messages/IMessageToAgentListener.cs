using Phantom.Common.Messages.ToAgent;

namespace Phantom.Common.Messages;

public interface IMessageToAgentListener {
	Task HandleRegisterAgentSuccessResult(RegisterAgentSuccessMessage message);
	Task HandleRegisterAgentFailureResult(RegisterAgentFailureMessage message);
	Task HandleConfigureInstance(ConfigureInstanceMessage message);
	Task HandleLaunchInstance(LaunchInstanceMessage message);
	Task HandleStopInstance(StopInstanceMessage message);
	Task HandleSendCommandToInstance(SendCommandToInstanceMessage message);
}
