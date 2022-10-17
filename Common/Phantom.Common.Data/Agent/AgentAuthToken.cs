using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using MessagePack;

namespace Phantom.Common.Data.Agent;

[MessagePackObject]
[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
public sealed class AgentAuthToken {
	internal const int Length = 12;

	[Key(0)]
	public byte[] Bytes { get; }

	public AgentAuthToken(byte[]? bytes) {
		if (bytes == null) {
			throw new ArgumentNullException(nameof(bytes));
		}

		if (bytes.Length != Length) {
			throw new ArgumentOutOfRangeException(nameof(bytes), "Invalid token length: " + bytes.Length + ". Token length must be exactly " + Length + " bytes.");
		}

		this.Bytes = bytes;
	}

	public bool FixedTimeEquals(AgentAuthToken providedAuthToken) {
		return CryptographicOperations.FixedTimeEquals(Bytes, providedAuthToken.Bytes);
	}

	internal void WriteTo(Span<byte> span) {
		Bytes.CopyTo(span);
	}

	public static AgentAuthToken Generate() {
		return new AgentAuthToken(RandomNumberGenerator.GetBytes(Length));
	}
}
