using NetMQ;
using NetMQ.Sockets;
using Phantom.Common.Messages.Agent;
using Phantom.Common.Messages.Agent.BiDirectional;
using Phantom.Common.Messages.Agent.ToController;
using Phantom.Utils.Rpc.Runtime;
using Phantom.Utils.Rpc.Sockets;
using Serilog;

namespace Phantom.Agent.Rpc;

public sealed class RpcClientRuntime : RpcClientRuntime<IMessageToAgentListener, IMessageToControllerListener, ReplyMessage> {
	public static Task Launch(RpcClientSocket<IMessageToAgentListener, IMessageToControllerListener, ReplyMessage> socket, IMessageToAgentListener messageListener, SemaphoreSlim disconnectSemaphore, CancellationToken receiveCancellationToken) {
		return new RpcClientRuntime(socket, messageListener, disconnectSemaphore, receiveCancellationToken).Launch();
	}

	private RpcClientRuntime(RpcClientSocket<IMessageToAgentListener, IMessageToControllerListener, ReplyMessage> socket, IMessageToAgentListener messageListener, SemaphoreSlim disconnectSemaphore, CancellationToken receiveCancellationToken) : base(socket, messageListener, disconnectSemaphore, receiveCancellationToken) {}

	protected override async Task RunWithConnection(ClientSocket socket, RpcConnectionToServer<IMessageToControllerListener> connection) {
		var keepAliveLoop = new KeepAliveLoop(connection);
		try {
			await base.RunWithConnection(socket, connection);
		} finally {
			keepAliveLoop.Cancel();
		}
	}

	protected override async Task SendDisconnectMessage(ClientSocket socket, ILogger logger) {
		var unregisterMessageBytes = AgentMessageRegistries.ToController.Write(new UnregisterAgentMessage()).ToArray();
		try {
			await socket.SendAsync(unregisterMessageBytes).AsTask().WaitAsync(TimeSpan.FromSeconds(5), CancellationToken.None);
		} catch (TimeoutException) {
			logger.Error("Timed out communicating agent shutdown with the controller.");
		}
	}
}
