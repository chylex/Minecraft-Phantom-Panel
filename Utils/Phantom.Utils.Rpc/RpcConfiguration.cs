using NetMQ;
using Serilog;

namespace Phantom.Utils.Rpc;

public sealed record RpcConfiguration(ILogger RuntimeLogger, ILogger TaskManagerLogger, string Host, ushort Port, NetMQCertificate ServerCertificate) {
	public string TcpUrl => "tcp://" + Host + ":" + Port;
}
