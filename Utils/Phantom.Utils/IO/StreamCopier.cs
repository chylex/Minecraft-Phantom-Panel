using System.Buffers;

namespace Phantom.Utils.IO;

public sealed class StreamCopier : IDisposable {
	private const int DefaultBufferSize = 81920;
	
	public event EventHandler<BufferEventArgs>? BufferReady;
	
	private readonly int bufferSize;
	
	public StreamCopier(int bufferSize = DefaultBufferSize) {
		this.bufferSize = bufferSize;
	}
	
	public async Task Copy(Stream source, Stream destination, CancellationToken cancellationToken) {
		byte[] buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
		try {
			int bytesRead;
			while ((bytesRead = await source.ReadAsync(buffer.AsMemory(), cancellationToken)) != 0) {
				var dataRead = new ReadOnlyMemory<byte>(buffer, 0, bytesRead);
				BufferReady?.Invoke(this, new BufferEventArgs(dataRead));
				await destination.WriteAsync(dataRead, cancellationToken);
			}
		} finally {
			ArrayPool<byte>.Shared.Return(buffer);
		}
	}
	
	public void Dispose() {
		BufferReady = null;
	}
	
	public sealed class BufferEventArgs : EventArgs {
		public ReadOnlyMemory<byte> Buffer { get; }
		
		internal BufferEventArgs(ReadOnlyMemory<byte> buffer) {
			Buffer = buffer;
		}
	}
}
