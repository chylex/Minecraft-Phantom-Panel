using NetMQ;
using NetMQ.Sockets;
using Phantom.Common.Rpc;

namespace Phantom.Server.Rpc;

public sealed class RpcLauncher : RpcRuntime<ServerSocket> {
	public static async Task Launch(RpcConfiguration config) {
		await new RpcLauncher(config).Launch();
	}

	private readonly RpcConfiguration config;
	
	private RpcLauncher(RpcConfiguration config) {
		this.config = config;
	}

	protected override void Connect(ServerSocket socket) {
		var logger = config.Logger;
		var url = config.TcpUrl;
		
		logger.Information("Starting ZeroMQ server on {Url}...", url);
		socket.Bind(url);
		logger.Information("ZeroMQ server initialized, listening for agent connections on port {Port}.", config.Port);
	}
	
	protected override async Task Run(ServerSocket socket) {
		await foreach (var (routingId, data) in socket.ReceiveStringAsyncEnumerable(config.CancellationToken)) {
			// TODO
			config.Logger.Verbose("Received message from {RoutingId}.", routingId);
			await socket.SendAsync(routingId, data + data);
		}
	}
}
