using System.Collections.Immutable;
using System.Security.Cryptography;

namespace Phantom.Utils.Rpc;

public sealed class AuthSecret {
	public const int Length = 12;
	
	public ImmutableArray<byte> Bytes { get; }
	
	public AuthSecret(ImmutableArray<byte> bytes) {
		if (bytes.Length != Length) {
			throw new ArgumentOutOfRangeException(nameof(bytes), "Invalid auth secret length: " + bytes.Length + ". Auth secret must be exactly " + Length + " bytes.");
		}
		
		this.Bytes = bytes;
	}
	
	internal bool FixedTimeEquals(AuthSecret provided) {
		return FixedTimeEquals(provided.Bytes.AsSpan());
	}
	
	internal bool FixedTimeEquals(ReadOnlySpan<byte> other) {
		return CryptographicOperations.FixedTimeEquals(Bytes.AsSpan(), other);
	}
	
	public static AuthSecret Generate() {
		return new AuthSecret([..RandomNumberGenerator.GetBytes(Length)]);
	}
}
