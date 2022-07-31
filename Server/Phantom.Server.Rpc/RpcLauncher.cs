using NetMQ;
using NetMQ.Sockets;
using Phantom.Common.Rpc;
using Phantom.Common.Rpc.Messages;

namespace Phantom.Server.Rpc;

public sealed class RpcLauncher : RpcRuntime<ServerSocket> {
	public static async Task Launch(RpcConfiguration config, Func<RpcClientConnection, IMessageToServerListener> listenerFactory) {
		await new RpcLauncher(config, listenerFactory).Launch();
	}

	private readonly RpcConfiguration config;
	private readonly Func<RpcClientConnection, IMessageToServerListener> listenerFactory;

	private RpcLauncher(RpcConfiguration config, Func<RpcClientConnection, IMessageToServerListener> listenerFactory) {
		this.config = config;
		this.listenerFactory = listenerFactory;
	}

	protected override void Connect(ServerSocket socket) {
		var logger = config.Logger;
		var url = config.TcpUrl;
		
		logger.Information("Starting ZeroMQ server on {Url}...", url);
		socket.Bind(url);
		logger.Information("ZeroMQ server initialized, listening for agent connections on port {Port}.", config.Port);
	}
	
	protected override async Task Run(ServerSocket socket) {
		var cancellationToken = config.CancellationToken;
		var listeners = new Dictionary<ulong, IMessageToServerListener>();
		
		// TODO optimize msg
		await foreach (var (routingId, bytes) in socket.ReceiveBytesAsyncEnumerable(cancellationToken)) {
			config.Logger.Verbose("Received {Bytes} B message from {RoutingId}.", bytes.Length, routingId);
			
			if (!listeners.TryGetValue(routingId, out var listener)) {
				listeners[routingId] = listener = listenerFactory(new RpcClientConnection(socket, routingId));
			}
			
			MessageRegistries.ToServer.Handle(bytes, listener, cancellationToken);
		}
	}
}
