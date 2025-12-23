using System.Net;
using Phantom.Utils.Rpc.Runtime.Tls;

namespace Phantom.Utils.Rpc.Runtime.Server;

public sealed record RpcServerConnectionParameters(
	EndPoint EndPoint,
	RpcServerCertificate Certificate,
	ushort PingIntervalSeconds,
	ushort MessageQueueCapacity,
	ushort FrameQueueCapacity,
	ushort MaxConcurrentlyHandledMessages
) : RpcCommonConnectionParameters(
	MessageQueueCapacity,
	FrameQueueCapacity,
	MaxConcurrentlyHandledMessages
) {
	internal TimeSpan PingInterval => TimeSpan.FromSeconds(PingIntervalSeconds);
}
