using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using MemoryPack;

namespace Phantom.Common.Data.Agent;

[MemoryPackable]
[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
public sealed partial class AgentAuthToken {
	internal const int Length = 12;

	[MemoryPackOrder(0)]
	[MemoryPackInclude]
	private readonly byte[] bytes;

	public AgentAuthToken(byte[]? bytes) {
		if (bytes == null) {
			throw new ArgumentNullException(nameof(bytes));
		}

		if (bytes.Length != Length) {
			throw new ArgumentOutOfRangeException(nameof(bytes), "Invalid token length: " + bytes.Length + ". Token length must be exactly " + Length + " bytes.");
		}

		this.bytes = bytes;
	}

	public bool FixedTimeEquals(AgentAuthToken providedAuthToken) {
		return CryptographicOperations.FixedTimeEquals(bytes, providedAuthToken.bytes);
	}

	internal void WriteTo(Span<byte> span) {
		bytes.CopyTo(span);
	}

	public static AgentAuthToken Generate() {
		return new AgentAuthToken(RandomNumberGenerator.GetBytes(Length));
	}
}
