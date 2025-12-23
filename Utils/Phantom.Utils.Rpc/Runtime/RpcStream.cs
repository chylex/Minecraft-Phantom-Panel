using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Net.Security;

namespace Phantom.Utils.Rpc.Runtime;

[SuppressMessage("ReSharper", "MemberCanBeInternal")]
public sealed class RpcStream : IAsyncDisposable {
	private readonly SslStream stream;
	
	internal RpcStream(SslStream stream) {
		this.stream = stream;
	}
	
	internal Task AuthenticateAsClient(SslClientAuthenticationOptions options, CancellationToken cancellationToken) {
		return stream.AuthenticateAsClientAsync(options, cancellationToken);
	}
	
	internal Task AuthenticateAsServer(SslServerAuthenticationOptions options, CancellationToken cancellationToken) {
		return stream.AuthenticateAsServerAsync(options, cancellationToken);
	}
	
	private async ValueTask WriteValue<T>(T value, int size, Action<Span<byte>, T> writer, CancellationToken cancellationToken) {
		using var buffer = RentedMemory<byte>.Rent(size);
		writer(buffer.AsSpan, value);
		await stream.WriteAsync(buffer.AsMemory, cancellationToken);
	}
	
	private async ValueTask<T> ReadValue<T>(Func<ReadOnlySpan<byte>, T> reader, int size, CancellationToken cancellationToken) {
		using var buffer = RentedMemory<byte>.Rent(size);
		await stream.ReadExactlyAsync(buffer.AsMemory, cancellationToken);
		return reader(buffer.AsSpan);
	}
	
	public ValueTask WriteByte(byte value, CancellationToken cancellationToken) {
		return WriteValue(value, sizeof(byte), static (span, value) => span[0] = value, cancellationToken);
	}
	
	public ValueTask<byte> ReadByte(CancellationToken cancellationToken) {
		return ReadValue(static span => span[0], sizeof(byte), cancellationToken);
	}
	
	public ValueTask WriteUnsignedShort(ushort value, CancellationToken cancellationToken) {
		return WriteValue(value, sizeof(ushort), BinaryPrimitives.WriteUInt16LittleEndian, cancellationToken);
	}
	
	public ValueTask<ushort> ReadUnsignedShort(CancellationToken cancellationToken) {
		return ReadValue(BinaryPrimitives.ReadUInt16LittleEndian, sizeof(ushort), cancellationToken);
	}
	
	public ValueTask WriteSignedInt(int value, CancellationToken cancellationToken) {
		return WriteValue(value, sizeof(int), BinaryPrimitives.WriteInt32LittleEndian, cancellationToken);
	}
	
	public ValueTask<int> ReadSignedInt(CancellationToken cancellationToken) {
		return ReadValue(BinaryPrimitives.ReadInt32LittleEndian, sizeof(int), cancellationToken);
	}
	
	public ValueTask WriteUnsignedInt(uint value, CancellationToken cancellationToken) {
		return WriteValue(value, sizeof(uint), BinaryPrimitives.WriteUInt32LittleEndian, cancellationToken);
	}
	
	public ValueTask<uint> ReadUnsignedInt(CancellationToken cancellationToken) {
		return ReadValue(BinaryPrimitives.ReadUInt32LittleEndian, sizeof(uint), cancellationToken);
	}
	
	public ValueTask WriteSignedLong(long value, CancellationToken cancellationToken) {
		return WriteValue(value, sizeof(long), BinaryPrimitives.WriteInt64LittleEndian, cancellationToken);
	}
	
	public ValueTask<long> ReadSignedLong(CancellationToken cancellationToken) {
		return ReadValue(BinaryPrimitives.ReadInt64LittleEndian, sizeof(long), cancellationToken);
	}
	
	public ValueTask WriteGuid(Guid guid, CancellationToken cancellationToken) {
		return WriteValue(guid, Serialization.GuidBytes, Serialization.WriteGuid, cancellationToken);
	}
	
	public ValueTask<Guid> ReadGuid(CancellationToken cancellationToken) {
		return ReadValue(static span => new Guid(span), Serialization.GuidBytes, cancellationToken);
	}
	
	public ValueTask WriteAuthToken(AuthToken authToken, CancellationToken cancellationToken) {
		return WriteValue(authToken, AuthToken.Length, static (span, value) => value.ToBytes(span), cancellationToken);
	}
	
	public ValueTask<AuthToken> ReadAuthToken(CancellationToken cancellationToken) {
		return ReadValue(AuthToken.FromBytes, AuthToken.Length, cancellationToken);
	}
	
	public ValueTask WriteBytes(ReadOnlyMemory<byte> bytes, CancellationToken cancellationToken) {
		return stream.WriteAsync(bytes, cancellationToken);
	}
	
	public ValueTask ReadBytes(Memory<byte> buffer, CancellationToken cancellationToken) {
		return stream.ReadExactlyAsync(buffer, cancellationToken);
	}
	
	public async ValueTask<ReadOnlyMemory<byte>> ReadBytes(int length, CancellationToken cancellationToken) {
		Memory<byte> buffer = new byte[length];
		await stream.ReadExactlyAsync(buffer, cancellationToken);
		return buffer;
	}
	
	public async ValueTask<ReadOnlyMemory<byte>> ReadBytes(uint length, CancellationToken cancellationToken) {
		Memory<byte> buffer = new byte[length];
		await stream.ReadExactlyAsync(buffer, cancellationToken);
		return buffer;
	}
	
	public Task Flush(CancellationToken cancellationToken) {
		return stream.FlushAsync(cancellationToken);
	}
	
	public async ValueTask DisposeAsync() {
		await stream.DisposeAsync();
	}
	
	private readonly record struct RentedMemory<T>(T[] Array, int Length) : IDisposable {
		public Span<T> AsSpan => Array.AsSpan(..Length);
		public Memory<T> AsMemory => Array.AsMemory(..Length);
		
		public void Dispose() {
			ArrayPool<T>.Shared.Return(Array);
		}
		
		public static RentedMemory<T> Rent(int bytes) {
			T[] buffer = ArrayPool<T>.Shared.Rent(bytes);
			try {
				return new RentedMemory<T>(buffer, bytes);
			} catch (Exception) {
				ArrayPool<T>.Shared.Return(buffer);
				throw;
			}
		}
	}
}
