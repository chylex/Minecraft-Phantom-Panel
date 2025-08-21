using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using MemoryPack;

namespace Phantom.Common.Data;

[MemoryPackable(GenerateType.VersionTolerant)]
[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
public sealed partial class AuthToken {
	internal const int Length = 12;
	
	[MemoryPackOrder(0)]
	[MemoryPackInclude]
	private readonly byte[] bytes;
	
	internal AuthToken(byte[]? bytes) {
		ArgumentNullException.ThrowIfNull(bytes);
		
		if (bytes.Length != Length) {
			throw new ArgumentOutOfRangeException(nameof(bytes), "Invalid token length: " + bytes.Length + ". Token length must be exactly " + Length + " bytes.");
		}
		
		this.bytes = bytes;
	}
	
	public bool FixedTimeEquals(AuthToken providedAuthToken) {
		return CryptographicOperations.FixedTimeEquals(bytes, providedAuthToken.bytes);
	}
	
	internal void WriteTo(Span<byte> span) {
		bytes.CopyTo(span);
	}
	
	public static AuthToken Generate() {
		return new AuthToken(RandomNumberGenerator.GetBytes(Length));
	}
}
