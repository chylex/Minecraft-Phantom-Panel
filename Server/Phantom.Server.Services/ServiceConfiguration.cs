using Phantom.Common.Data;

namespace Phantom.Server.Services;

public static class ServiceConfiguration {
	public static AgentAuthToken? AgentToken { get; set; }

	internal sealed record ValidatedConfiguration(
		AgentAuthToken AuthToken
	);

	internal static ValidatedConfiguration Validate() {
		return new ValidatedConfiguration(AgentToken ?? throw InvalidException("AgentToken is not set."));
	}
	
	private static Exception InvalidException(string message) {
		return new InvalidOperationException("Invalid server configuration: " + message);
	}
}
