using System.Buffers;
using System.Security.Cryptography;

namespace Phantom.Utils.Cryptography;

public static class StreamHasher {
	private const int CopyBufferSize = 81920;

	public static async Task<Sha1String> Copy(Stream source, Stream destination, CancellationToken cancellationToken) {
		IncrementalHash hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA1);

		byte[] buffer = ArrayPool<byte>.Shared.Rent(CopyBufferSize);
		try {
			int bytesRead;
			while ((bytesRead = await source.ReadAsync(buffer.AsMemory(), cancellationToken)) != 0) {
				var dataRead = new ReadOnlyMemory<byte>(buffer, 0, bytesRead);
				hash.AppendData(dataRead.Span);
				await destination.WriteAsync(dataRead, cancellationToken);
			}
		} finally {
			ArrayPool<byte>.Shared.Return(buffer);
		}

		return Sha1String.FromBytes(hash.GetHashAndReset());
	}
}
