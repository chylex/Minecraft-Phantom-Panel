using Phantom.Common.Messages.Web.BiDirectional;
using Phantom.Common.Messages.Web.ToWeb;
using Phantom.Utils.Rpc.Message;

namespace Phantom.Common.Messages.Web;

public interface IMessageToWebListener {
	Task<NoReply> HandleRegisterWebResult(RegisterWebResultMessage message);
	Task<NoReply> HandleRefreshAgents(RefreshAgentsMessage message);
	Task<NoReply> HandleRefreshInstances(RefreshInstancesMessage message);
	Task<NoReply> HandleInstanceOutput(InstanceOutputMessage message);
	Task<NoReply> HandleReply(ReplyMessage message);
}
