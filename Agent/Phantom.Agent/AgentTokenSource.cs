using Phantom.Common.Data;

namespace Phantom.Agent;

readonly struct AgentTokenSource {
	public static async Task<AgentAuthToken> Read(string? token, string? tokenFilePath) {
		if (token != null) {
			return AgentAuthToken.FromString(token);
		}
		else if (tokenFilePath != null) {
			return await AgentAuthToken.ReadFromFile(tokenFilePath);
		}
		else {
			throw new InvalidOperationException();
		}
	}
}
