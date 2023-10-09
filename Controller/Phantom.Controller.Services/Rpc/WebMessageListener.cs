using Phantom.Common.Messages.Web;
using Phantom.Common.Messages.Web.BiDirectional;
using Phantom.Controller.Rpc;
using Phantom.Utils.Rpc.Message;

namespace Phantom.Controller.Services.Rpc;

public sealed class WebMessageListener : IMessageToControllerListener {
	private readonly RpcClientConnection<IMessageToWebListener> connection;
	
	internal WebMessageListener(RpcClientConnection<IMessageToWebListener> connection) {
		this.connection = connection;
	}

	public Task<NoReply> HandleReply(ReplyMessage message) {
		connection.Receive(message);
		return Task.FromResult(NoReply.Instance);
	}
}
