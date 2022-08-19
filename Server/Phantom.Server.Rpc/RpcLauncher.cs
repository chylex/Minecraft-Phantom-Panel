using NetMQ;
using NetMQ.Sockets;
using Phantom.Common.Messages;
using Phantom.Utils.Rpc;

namespace Phantom.Server.Rpc;

public sealed class RpcLauncher : RpcRuntime<ServerSocket> {
	public static async Task Launch(RpcConfiguration config, Func<RpcClientConnection, IMessageToServerListener> listenerFactory) {
		var socket = new ServerSocket();
		var options = socket.Options;
		
		options.CurveServer = true;
		options.CurveCertificate = config.ServerCertificate;
		
		await new RpcLauncher(config, socket, listenerFactory).Launch();
	}

	private readonly RpcConfiguration config;
	private readonly Func<RpcClientConnection, IMessageToServerListener> listenerFactory;

	private RpcLauncher(RpcConfiguration config, ServerSocket socket, Func<RpcClientConnection, IMessageToServerListener> listenerFactory) : base(socket) {
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
		var clients = new Dictionary<ulong, Client>();
		
		// TODO optimize msg
		await foreach (var (routingId, bytes) in socket.ReceiveBytesAsyncEnumerable(cancellationToken)) {
			config.Logger.Verbose("Received {Bytes} B message from {RoutingId}.", bytes.Length, routingId);

			if (bytes.Length == 0) {
				continue;
			}
			
			if (!clients.TryGetValue(routingId, out var client)) {
				clients[routingId] = client = new Client {
					Connection = new RpcClientConnection(socket, routingId),
					Listener = listenerFactory(new RpcClientConnection(socket, routingId))
				};
			}

			MessageRegistries.ToServer.Handle(bytes, client.Listener, cancellationToken);

			if (client.Listener.IsDisposed) {
				client.Connection.IsClosed = true;
				clients.Remove(routingId);
				config.Logger.Verbose("Closed connection to {RoutingId}.", routingId);
			}
		}
	}

	private readonly struct Client {
		public RpcClientConnection Connection { get; init; }
		public IMessageToServerListener Listener { get; init; }
	}
}
