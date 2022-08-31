using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;
using MessagePack;
using Phantom.Utils.Cryptography;
using Phantom.Utils.IO;

namespace Phantom.Common.Data.Agent;

[MessagePackObject]
[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
public sealed class AgentAuthToken {
	private const int MinimumTokenLength = 30;
	private const int MaximumTokenLength = 100;

	[Key(0)]
	public string Value { get; }

	[IgnoreMember]
	private readonly byte[] bytes;

	public AgentAuthToken(string? value) {
		if (value == null) {
			throw new ArgumentNullException(nameof(value));
		}

		if (value.Length is < MinimumTokenLength or > MaximumTokenLength) {
			throw new ArgumentOutOfRangeException(nameof(value), "Invalid token length: " + value.Length + ". Token length must be between " + MinimumTokenLength + " and " + MaximumTokenLength + ".");
		}

		this.Value = value;
		this.bytes = TokenGenerator.GetBytesOrThrow(value);
	}

	public bool FixedTimeEquals(AgentAuthToken providedAuthToken) {
		return CryptographicOperations.FixedTimeEquals(bytes, providedAuthToken.bytes);
	}

	public override string ToString() {
		return Value;
	}

	public async Task WriteToFile(string filePath) {
		await Files.WriteBytesAsync(filePath, bytes, FileMode.Create, Chmod.URW_GR);
	}

	public static async Task<AgentAuthToken> ReadFromFile(string filePath) {
		Files.RequireMaximumFileSize(filePath, MaximumTokenLength + 1);
		string contents = await File.ReadAllTextAsync(filePath, Encoding.ASCII);
		return new AgentAuthToken(contents.Trim());
	}

	public static AgentAuthToken Generate() {
		return new AgentAuthToken(TokenGenerator.Create(MinimumTokenLength));
	}
}
