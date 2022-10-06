using Phantom.Common.Logging;
using Phantom.Utils.Runtime;

namespace Phantom.Agent;

sealed record Variables(
	string? AuthToken,
	string? AuthTokenFilePath
) {
	private static Variables LoadOrThrow() {
		var (authToken, authTokenFilePath) = EnvironmentVariables.GetEitherString("SERVER_AUTH_TOKEN", "SERVER_AUTH_TOKEN_FILE").OrThrow;

		return new Variables(
			authToken,
			authTokenFilePath
		);
	}

	public static Variables LoadOrExit() {
		try {
			return LoadOrThrow();
		} catch (Exception e) {
			PhantomLogger.Root.Fatal(e.Message);
			Environment.Exit(1);
			throw;
		}
	}
}
