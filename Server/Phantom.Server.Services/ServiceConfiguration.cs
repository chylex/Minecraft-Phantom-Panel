using Phantom.Common.Data;

namespace Phantom.Server.Services;

public static class ServiceConfiguration {
	public static AgentAuthToken? AgentToken { get; set; }
	public static CancellationToken? CancellationToken { get; set; }

	internal sealed record ValidatedConfiguration(
		AgentAuthToken AuthToken,
		CancellationToken CancellationToken
	);

	internal static ValidatedConfiguration Validate() {
		return new ValidatedConfiguration(
			AgentToken ?? throw InvalidException("AgentToken is not set."),
			CancellationToken ?? throw InvalidException("CancellationToken is not set.")
		);
	}
	
	private static Exception InvalidException(string message) {
		return new InvalidOperationException("Invalid server configuration: " + message);
	}
}
