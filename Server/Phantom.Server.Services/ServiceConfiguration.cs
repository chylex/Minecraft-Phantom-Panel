using Phantom.Server.Services.Agents;

namespace Phantom.Server.Services;

public static class ServiceConfiguration {
	public static string? AgentToken { get; set; }

	internal sealed record ValidatedConfiguration(
		AgentAuthToken AuthToken
	);

	internal static ValidatedConfiguration Validate() {
		return new ValidatedConfiguration(AgentAuthToken.From(AgentToken));
	}
}
