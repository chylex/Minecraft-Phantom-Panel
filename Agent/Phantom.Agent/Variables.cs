using Phantom.Common.Data;
using Phantom.Utils.Logging;
using Phantom.Utils.Runtime;

namespace Phantom.Agent;

sealed record Variables(
	string ServerHost,
	ushort ServerPort,
	string? AuthToken,
	string? AuthTokenFilePath,
	string AgentName,
	ushort MaxInstances,
	RamAllocationUnits MaxMemory
) {
	private static Variables LoadOrThrow(Func<string> defaultAgentName) {
		var (authToken, authTokenFilePath) = EnvironmentVariables.GetEitherString("SERVER_AUTH_TOKEN", "SERVER_AUTH_TOKEN_FILE").OrThrow;
		
		return new Variables(
			EnvironmentVariables.GetString("SERVER_HOST").OrThrow,
			EnvironmentVariables.GetPortNumber("SERVER_PORT").OrDefault(9401),
			authToken,
			authTokenFilePath,
			EnvironmentVariables.GetString("AGENT_NAME").OrGetDefault(defaultAgentName),
			(ushort) EnvironmentVariables.GetInteger("MAX_INSTANCES").OrThrow, // TODO
			RamAllocationUnits.FromString(EnvironmentVariables.GetString("MAX_MEMORY").OrThrow)
		);
	}

	public static Variables LoadOrExit(Func<string> defaultAgentName) {
		try {
			return LoadOrThrow(defaultAgentName);
		} catch (Exception e) {
			PhantomLogger.Root.Fatal(e.Message);
			Environment.Exit(1);
			throw;
		}
	}
}
