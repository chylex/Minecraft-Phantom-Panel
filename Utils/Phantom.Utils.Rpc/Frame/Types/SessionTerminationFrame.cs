using Phantom.Utils.Rpc.Runtime;

namespace Phantom.Utils.Rpc.Frame.Types;

sealed record SessionTerminationFrame : IFrame {
	public static SessionTerminationFrame Instance { get; } = new ();
	
	public ReadOnlyMemory<byte> FrameType => IFrame.TypeSessionTermination;
	
	public Task Write(RpcStream stream, CancellationToken cancellationToken) {
		return Task.CompletedTask;
	}
}
