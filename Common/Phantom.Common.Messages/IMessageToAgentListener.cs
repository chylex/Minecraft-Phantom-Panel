using Phantom.Common.Data.Replies;
using Phantom.Common.Messages.BiDirectional;
using Phantom.Common.Messages.ToAgent;
using Phantom.Utils.Rpc.Message;

namespace Phantom.Common.Messages;

public interface IMessageToAgentListener {
	Task<NoReply> HandleRegisterAgentSuccess(RegisterAgentSuccessMessage message);
	Task<NoReply> HandleRegisterAgentFailure(RegisterAgentFailureMessage message);
	Task<InstanceActionResult<ConfigureInstanceResult>> HandleConfigureInstance(ConfigureInstanceMessage message);
	Task<InstanceActionResult<LaunchInstanceResult>> HandleLaunchInstance(LaunchInstanceMessage message);
	Task<InstanceActionResult<StopInstanceResult>> HandleStopInstance(StopInstanceMessage message);
	Task<InstanceActionResult<SendCommandToInstanceResult>> HandleSendCommandToInstance(SendCommandToInstanceMessage message);
	Task<NoReply> HandleReply(ReplyMessage message);
}
