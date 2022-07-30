using Serilog;

namespace Phantom.Common.Rpc;

public sealed record RpcConfiguration(ILogger Logger, string Host, ushort Port, CancellationToken CancellationToken) {
	public string TcpUrl => "tcp://" + Host + ":" + Port;
}
