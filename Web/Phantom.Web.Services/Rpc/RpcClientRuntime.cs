using NetMQ;
using NetMQ.Sockets;
using Phantom.Common.Messages.Web;
using Phantom.Common.Messages.Web.BiDirectional;
using Phantom.Common.Messages.Web.ToController;
using Phantom.Utils.Rpc.Runtime;
using Phantom.Utils.Rpc.Sockets;
using ILogger = Serilog.ILogger;

namespace Phantom.Web.Services.Rpc;

public sealed class RpcClientRuntime : RpcClientRuntime<IMessageToWebListener, IMessageToControllerListener, ReplyMessage> {
	public static Task Launch(RpcClientSocket<IMessageToWebListener, IMessageToControllerListener, ReplyMessage> socket, IMessageToWebListener messageListener, SemaphoreSlim disconnectSemaphore, CancellationToken receiveCancellationToken) {
		return new RpcClientRuntime(socket, messageListener, disconnectSemaphore, receiveCancellationToken).Launch();
	}

	private RpcClientRuntime(RpcClientSocket<IMessageToWebListener, IMessageToControllerListener, ReplyMessage> socket, IMessageToWebListener messageListener, SemaphoreSlim disconnectSemaphore, CancellationToken receiveCancellationToken) : base(socket, messageListener, disconnectSemaphore, receiveCancellationToken) {}
	
	protected override async Task Disconnect(ClientSocket socket, ILogger logger) {
		var unregisterMessageBytes = WebMessageRegistries.ToController.Write(new UnregisterWebMessage()).ToArray();
		try {
			await socket.SendAsync(unregisterMessageBytes).AsTask().WaitAsync(TimeSpan.FromSeconds(5), CancellationToken.None);
		} catch (TimeoutException) {
			logger.Error("Timed out communicating web shutdown with the controller.");
		}
	}
}
