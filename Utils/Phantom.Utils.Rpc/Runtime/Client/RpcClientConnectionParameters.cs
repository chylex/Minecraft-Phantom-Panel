using Phantom.Utils.Rpc.Runtime.Tls;

namespace Phantom.Utils.Rpc.Runtime.Client;

public sealed record RpcClientConnectionParameters(
	string Host,
	ushort Port,
	string DistinguishedName,
	RpcCertificateThumbprint CertificateThumbprint,
	AuthToken AuthToken,
	IRpcClientHandshake Handshake,
	ushort MessageQueueCapacity,
	ushort FrameQueueCapacity,
	ushort MaxConcurrentlyHandledMessages
) : RpcCommonConnectionParameters(
	MessageQueueCapacity,
	FrameQueueCapacity,
	MaxConcurrentlyHandledMessages
);
