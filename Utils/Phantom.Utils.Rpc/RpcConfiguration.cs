using NetMQ;

namespace Phantom.Utils.Rpc;

public sealed record RpcConfiguration(string ServiceName, string Host, ushort Port, NetMQCertificate ServerCertificate) {
	internal string LoggerName => "Rpc:" + ServiceName;
	internal string TcpUrl => "tcp://" + Host + ":" + Port;
}
