using Phantom.Utils.Rpc.Runtime;

namespace Phantom.Utils.Rpc.Frame.Types;

sealed record PongFrame(DateTimeOffset PingTime) : IFrame {
	public ReadOnlyMemory<byte> FrameType => IFrame.TypePong;
	
	public async Task Write(RpcStream stream, CancellationToken cancellationToken) {
		await stream.WriteSignedLong(PingTime.ToUnixTimeMilliseconds(), cancellationToken);
	}
	
	public static async Task<PongFrame> Read(RpcStream stream, CancellationToken cancellationToken) {
		return new PongFrame(DateTimeOffset.FromUnixTimeMilliseconds(await stream.ReadSignedLong(cancellationToken)));
	}
}
