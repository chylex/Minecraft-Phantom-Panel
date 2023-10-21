using NetMQ.Sockets;

namespace Phantom.Utils.Rpc.Sockets; 

public sealed class RpcServerSocket : RpcSocket<ServerSocket> {
	public static RpcServerSocket Connect(RpcConfiguration config) {
		var socket = new ServerSocket();
		var options = socket.Options;

		options.CurveServer = true;
		options.CurveCertificate = config.ServerCertificate;
		RpcSocket.SetDefaultSocketOptions(options);

		var url = config.TcpUrl;
		var logger = config.RuntimeLogger;
		
		logger.Information("Starting ZeroMQ server on {Url}...", url);
		socket.Bind(url);
		logger.Information("ZeroMQ server initialized, listening for connections on port {Port}.", config.Port);
		
		return new RpcServerSocket(socket, config);
	}

	private RpcServerSocket(ServerSocket socket, RpcConfiguration config) : base(socket, config) {}
}
