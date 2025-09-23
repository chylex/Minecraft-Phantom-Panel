using Phantom.Utils.Rpc.Runtime;

namespace Phantom.Utils.Rpc.Frame.Types;

sealed record PingFrame : IFrame {
	public static PingFrame Instance { get; } = new ();
	
	public ReadOnlyMemory<byte> FrameType => IFrame.TypePing;
	
	public async Task Write(RpcStream stream, CancellationToken cancellationToken) {
		await stream.WriteSignedLong(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), cancellationToken);
	}
	
	public static async Task<DateTimeOffset> Read(RpcStream stream, CancellationToken cancellationToken) {
		return DateTimeOffset.FromUnixTimeMilliseconds(await stream.ReadSignedLong(cancellationToken));
	}
}
