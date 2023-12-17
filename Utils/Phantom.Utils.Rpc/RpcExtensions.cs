using NetMQ;
using NetMQ.Sockets;

namespace Phantom.Utils.Rpc;

static class RpcExtensions {
	public static ReadOnlyMemory<byte> Receive(this ClientSocket socket, CancellationToken cancellationToken) {
		var msg = new Msg();
		msg.InitEmpty();
		
		try {
			socket.Receive(ref msg, cancellationToken);
			return msg.SliceAsMemory();
		} finally {
			// Only releases references, so the returned ReadOnlyMemory is safe.
			msg.Close();
		}
	}
	
	public static (uint, ReadOnlyMemory<byte>) Receive(this ServerSocket socket, CancellationToken cancellationToken) {
		var msg = new Msg();
		msg.InitEmpty();
		
		try {
			socket.Receive(ref msg, cancellationToken);
			return (msg.RoutingId, msg.SliceAsMemory());
		} finally {
			// Only releases references, so the returned ReadOnlyMemory is safe.
			msg.Close();
		}
	}
}
