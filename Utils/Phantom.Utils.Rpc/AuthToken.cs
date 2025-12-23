using System.Collections.Immutable;

namespace Phantom.Utils.Rpc;

public sealed record AuthToken(Guid Guid, AuthSecret Secret) {
	public const int Length = Serialization.GuidBytes + AuthSecret.Length;
	
	public ImmutableArray<byte> ToBytes() {
		Span<byte> buffer = stackalloc byte[Length];
		ToBytes(buffer);
		return [..buffer];
	}
	
	public void ToBytes(Span<byte> buffer) {
		Serialization.WriteGuid(buffer, Guid);
		Secret.Bytes.CopyTo(buffer[Serialization.GuidBytes..]);
	}
	
	public static AuthToken FromBytes(ReadOnlySpan<byte> bytes) {
		if (bytes.Length != Length) {
			throw new ArgumentOutOfRangeException(nameof(bytes), "Invalid auth token length: " + bytes.Length + ". Auth token must be exactly " + Length + " bytes.");
		}
		
		var guidSpan = bytes[..Serialization.GuidBytes];
		var secretSpan = bytes[Serialization.GuidBytes..];
		
		var guid = new Guid(guidSpan);
		var secret = new AuthSecret([..secretSpan]);
		return new AuthToken(guid, secret);
	}
	
	public static AuthToken Generate() {
		return new AuthToken(Guid.NewGuid(), AuthSecret.Generate());
	}
}
