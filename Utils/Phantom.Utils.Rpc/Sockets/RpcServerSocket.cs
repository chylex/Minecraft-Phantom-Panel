using NetMQ.Sockets;
using Phantom.Utils.Logging;

namespace Phantom.Utils.Rpc.Sockets;

sealed class RpcServerSocket : RpcSocket<ServerSocket> {
	public static RpcServerSocket Connect(RpcConfiguration config) {
		var socket = new ServerSocket();
		var options = socket.Options;
		
		options.CurveServer = true;
		options.CurveCertificate = config.ServerCertificate;
		RpcSocket.SetDefaultSocketOptions(options);
		
		var url = config.TcpUrl;
		var logger = PhantomLogger.Create(config.LoggerName);
		
		logger.Information("Starting ZeroMQ server on {Url}...", url);
		socket.Bind(url);
		logger.Information("ZeroMQ server initialized, listening for connections on port {Port}.", config.Port);
		
		return new RpcServerSocket(socket, config);
	}
	
	private RpcServerSocket(ServerSocket socket, RpcConfiguration config) : base(socket, config) {}
}
