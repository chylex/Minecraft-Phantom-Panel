using Phantom.Common.Messages.ToAgent;

namespace Phantom.Common.Messages;

public interface IMessageToAgentListener {
	Task HandleRegisterAgentSuccessResult(RegisterAgentSuccessMessage message);
	Task HandleRegisterAgentFailureResult(RegisterAgentFailureMessage message);
}
