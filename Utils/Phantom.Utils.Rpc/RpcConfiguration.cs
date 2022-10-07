using NetMQ;
using Phantom.Utils.Threading;
using Serilog;

namespace Phantom.Utils.Rpc;

public sealed record RpcConfiguration(ILogger Logger, string Host, ushort Port, NetMQCertificate ServerCertificate, TaskManager TaskManager, CancellationToken CancellationToken) {
	public string TcpUrl => "tcp://" + Host + ":" + Port;
}
