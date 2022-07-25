using NetMQ;
using NetMQ.Sockets;

namespace Phantom.Server.Rpc;

public static class Launcher {
	public static async Task Launch(Configuration config) {
		config.Logger.Information("Starting ZeroMQ server on {Url}...", config.TcpUrl);
		
		using var socket = new ServerSocket();
		socket.Bind(config.TcpUrl);
		
		config.Logger.Information("ZeroMQ server initialized, listening for agent connections on port {Port}.", config.Port);

		try {
			await Run(socket, config.CancellationToken);
		} catch (OperationCanceledException) {
			// ignore
		}
	}

	private static async Task Run(ServerSocket socket, CancellationToken cancellationToken) {
		await foreach (var (routingId, data) in socket.ReceiveStringAsyncEnumerable(cancellationToken)) {
			await socket.SendAsync(routingId, data + data);
		}
	}
}
