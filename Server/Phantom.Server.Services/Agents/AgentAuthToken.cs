using System.Security.Cryptography;
using Phantom.Utils.Cryptography;

namespace Phantom.Server.Services.Agents;

public sealed class AgentAuthToken {
	internal static AgentAuthToken From(string? authToken) {
		if (string.IsNullOrEmpty(authToken)) {
			throw new Exception("Agent authentication token is not set.");
		}

		try {
			return new AgentAuthToken(authToken);
		} catch (Exception) {
			throw new Exception("Agent authentication token is invalid: " + authToken);
		}
	}

	private readonly string authToken;
	private readonly byte[] authTokenBytes;

	private AgentAuthToken(string authToken) {
		this.authToken = authToken;
		this.authTokenBytes = TokenGenerator.GetBytesOrThrow(authToken);
	}

	internal bool Check(string providedAuthToken) {
		byte[]? providedAuthTokenBytes;
		try {
			providedAuthTokenBytes = TokenGenerator.GetBytesOrThrow(providedAuthToken);
		} catch (Exception) {
			providedAuthTokenBytes = null;
		}

		return CryptographicOperations.FixedTimeEquals(providedAuthTokenBytes, authTokenBytes);
	}

	public override string ToString() {
		return authToken;
	}
}
