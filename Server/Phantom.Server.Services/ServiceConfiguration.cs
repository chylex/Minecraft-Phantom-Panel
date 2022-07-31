namespace Phantom.Server.Services; 

public static class ServiceConfiguration {
	public static string? AuthToken { get; set; }

	internal sealed record ValidatedConfiguration(
		string AuthToken
	);
	
	internal static ValidatedConfiguration Validate() {
		if (string.IsNullOrWhiteSpace(AuthToken)) {
			throw new Exception("Agent authentication token is invalid: " + AuthToken);
		}
		
		return new ValidatedConfiguration(AuthToken);
	}
}
