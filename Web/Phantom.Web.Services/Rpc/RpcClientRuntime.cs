using NetMQ;
using NetMQ.Sockets;
using Phantom.Common.Messages.Web;
using Phantom.Common.Messages.Web.BiDirectional;
using Phantom.Common.Messages.Web.ToController;
using Phantom.Utils.Actor;
using Phantom.Utils.Rpc.Runtime;
using Phantom.Utils.Rpc.Sockets;
using ILogger = Serilog.ILogger;

namespace Phantom.Web.Services.Rpc;

public sealed class RpcClientRuntime : RpcClientRuntime<IMessageToWeb, IMessageToController, ReplyMessage> {
	public static Task Launch(RpcClientSocket<IMessageToWeb, IMessageToController, ReplyMessage> socket, ActorRef<IMessageToWeb> handlerActorRef, SemaphoreSlim disconnectSemaphore, CancellationToken receiveCancellationToken) {
		return new RpcClientRuntime(socket, handlerActorRef, disconnectSemaphore, receiveCancellationToken).Launch();
	}
	
	private RpcClientRuntime(RpcClientSocket<IMessageToWeb, IMessageToController, ReplyMessage> socket, ActorRef<IMessageToWeb> handlerActor, SemaphoreSlim disconnectSemaphore, CancellationToken receiveCancellationToken) : base(socket, handlerActor, disconnectSemaphore, receiveCancellationToken) {}
	
	protected override async Task SendDisconnectMessage(ClientSocket socket, ILogger logger) {
		var unregisterMessageBytes = WebMessageRegistries.ToController.Write(new UnregisterWebMessage()).ToArray();
		try {
			await socket.SendAsync(unregisterMessageBytes).AsTask().WaitAsync(TimeSpan.FromSeconds(5), CancellationToken.None);
		} catch (TimeoutException) {
			logger.Error("Timed out communicating web shutdown with the controller.");
		}
	}
}
