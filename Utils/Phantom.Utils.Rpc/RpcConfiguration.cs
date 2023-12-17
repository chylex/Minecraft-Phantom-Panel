using NetMQ;

namespace Phantom.Utils.Rpc;

public sealed record RpcConfiguration(string LoggerName, string Host, ushort Port, NetMQCertificate ServerCertificate) {
	public string TcpUrl => "tcp://" + Host + ":" + Port;
}
