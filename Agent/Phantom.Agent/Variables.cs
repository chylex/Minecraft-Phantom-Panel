using Phantom.Common.Data;
using Phantom.Common.Logging;
using Phantom.Utils.Runtime;

namespace Phantom.Agent;

sealed record Variables(
	string ServerHost,
	ushort ServerPort,
	string? AuthToken,
	string? AuthTokenFilePath,
	string AgentNameOrEmpty,
	ushort MaxInstances,
	RamAllocationUnits MaxMemory
) {
	private static Variables LoadOrThrow() {
		var (authToken, authTokenFilePath) = EnvironmentVariables.GetEitherString("SERVER_AUTH_TOKEN", "SERVER_AUTH_TOKEN_FILE").OrThrow;
		
		return new Variables(
			EnvironmentVariables.GetString("SERVER_HOST").OrThrow,
			EnvironmentVariables.GetPortNumber("SERVER_PORT").OrDefault(9401),
			authToken,
			authTokenFilePath,
			EnvironmentVariables.GetString("AGENT_NAME").OrDefault(string.Empty),
			(ushort) EnvironmentVariables.GetInteger("MAX_INSTANCES").OrThrow, // TODO
			RamAllocationUnits.FromString(EnvironmentVariables.GetString("MAX_MEMORY").OrThrow)
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
