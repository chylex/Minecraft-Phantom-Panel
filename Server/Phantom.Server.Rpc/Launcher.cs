using NetMQ;
using NetMQ.Sockets;
using ILogger = Serilog.ILogger;

namespace Phantom.Server.Rpc;

public static class Launcher {
	public static async Task Launch(Configuration config) {
		var logger = config.Logger;
		
		logger.Information("Starting ZeroMQ server on {Url}...", config.TcpUrl);
		
		using var socket = new ServerSocket();
		socket.Bind(config.TcpUrl);
		
		logger.Information("ZeroMQ server initialized, listening for agent connections on port {Port}.", config.Port);

		try {
			await Run(logger, socket, config.CancellationToken);
		} catch (OperationCanceledException) {
			// ignore
		} finally {
			socket.Dispose();
			NetMQConfig.Cleanup();
		}
	}

	private static async Task Run(ILogger logger, ServerSocket socket, CancellationToken cancellationToken) {
		await foreach (var (routingId, data) in socket.ReceiveStringAsyncEnumerable(cancellationToken)) {
			// TODO
			logger.Verbose("Received message from {RoutingId}.", routingId);
			await socket.SendAsync(routingId, data + data);
		}
	}
}
