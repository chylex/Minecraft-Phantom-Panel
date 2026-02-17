using System.Buffers;

namespace Phantom.Utils.IO;

public sealed class StreamCopier(int bufferSize = StreamCopier.DefaultBufferSize) : IDisposable {
	private const int DefaultBufferSize = 81920;
	
	public event EventHandler<BufferEventArgs>? BufferReady;
	
	private readonly byte[] buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
	
	public async Task Copy(Stream source, Stream destination, CancellationToken cancellationToken) {
		int bytesRead;
		while ((bytesRead = await source.ReadAsync(buffer.AsMemory(), cancellationToken)) != 0) {
			var dataRead = new ReadOnlyMemory<byte>(buffer, start: 0, bytesRead);
			BufferReady?.Invoke(this, new BufferEventArgs(dataRead));
			await destination.WriteAsync(dataRead, cancellationToken);
		}
	}
	
	public void Dispose() {
		ArrayPool<byte>.Shared.Return(buffer);
		BufferReady = null;
	}
	
	public sealed class BufferEventArgs : EventArgs {
		public ReadOnlyMemory<byte> Buffer { get; }
		
		internal BufferEventArgs(ReadOnlyMemory<byte> buffer) {
			Buffer = buffer;
		}
	}
}
