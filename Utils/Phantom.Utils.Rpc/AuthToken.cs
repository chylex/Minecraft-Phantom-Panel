using System.Collections.Immutable;
using System.Security.Cryptography;

namespace Phantom.Utils.Rpc;

public sealed class AuthToken {
	public const int Length = 12;
	
	public ImmutableArray<byte> Bytes { get; }
	
	public AuthToken(ImmutableArray<byte> bytes) {
		if (bytes.Length != Length) {
			throw new ArgumentOutOfRangeException(nameof(bytes), "Invalid token length: " + bytes.Length + ". Token length must be exactly " + Length + " bytes.");
		}
		
		this.Bytes = bytes;
	}
	
	internal bool FixedTimeEquals(AuthToken providedAuthToken) {
		return FixedTimeEquals(providedAuthToken.Bytes.AsSpan());
	}
	
	public bool FixedTimeEquals(ReadOnlySpan<byte> other) {
		return CryptographicOperations.FixedTimeEquals(Bytes.AsSpan(), other);
	}
	
	public static AuthToken Generate() {
		return new AuthToken([..RandomNumberGenerator.GetBytes(Length)]);
	}
}
