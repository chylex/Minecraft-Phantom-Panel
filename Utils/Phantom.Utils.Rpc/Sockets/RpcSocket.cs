using NetMQ;
using Phantom.Utils.Rpc.Message;

namespace Phantom.Utils.Rpc.Sockets;

static class RpcSocket {
	internal static void SetDefaultSocketOptions(ThreadSafeSocketOptions options) {
		// TODO test behavior when either agent or server are offline for a very long time
		options.DelayAttachOnConnect = true;
		options.ReceiveHighWatermark = 10_000;
		options.SendHighWatermark = 10_000;
	}
}

public abstract class RpcSocket<TSocket> where TSocket : ThreadSafeSocket {
	internal TSocket Socket { get; }
	internal RpcConfiguration Config { get; }
	internal MessageReplyTracker ReplyTracker { get; }

	protected RpcSocket(TSocket socket, RpcConfiguration config) {
		Socket = socket;
		Config = config;
		ReplyTracker = new MessageReplyTracker(config.LoggerName);
	}
}
