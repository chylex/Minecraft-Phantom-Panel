namespace Phantom.Utils.Rpc.Runtime;

public record RpcCommonConnectionParameters(
	ushort MessageQueueCapacity,
	ushort FrameQueueCapacity,
	ushort MaxConcurrentlyHandledMessages
);
