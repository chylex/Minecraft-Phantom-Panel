using ILogger = Serilog.ILogger;

namespace Phantom.Server.Rpc;

public sealed record Configuration(ILogger Logger, string Host, ushort Port, CancellationToken CancellationToken) {
	internal string TcpUrl => "tcp://" + Host + ":" + Port;
}
