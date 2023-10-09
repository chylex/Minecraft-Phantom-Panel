using Phantom.Common.Messages.Web.BiDirectional;
using Phantom.Utils.Rpc.Message;

namespace Phantom.Common.Messages.Web;

public interface IMessageToWebListener {
	Task<NoReply> HandleReply(ReplyMessage message);
}
