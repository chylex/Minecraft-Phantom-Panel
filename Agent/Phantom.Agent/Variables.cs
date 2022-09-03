using Phantom.Agent.Minecraft.Java;
using Phantom.Common.Data.Instance;
using Phantom.Common.Logging;
using Phantom.Utils.Runtime;

namespace Phantom.Agent;

sealed record Variables(
	string ServerHost,
	ushort ServerPort,
	string JavaSearchPath,
	string? AuthToken,
	string? AuthTokenFilePath,
	string AgentNameOrEmpty,
	ushort MaxInstances,
	RamAllocationUnits MaxMemory
) {
	private static Variables LoadOrThrow() {
		var (authToken, authTokenFilePath) = EnvironmentVariables.GetEitherString("SERVER_AUTH_TOKEN", "SERVER_AUTH_TOKEN_FILE").OrThrow;
		var javaSearchPath = EnvironmentVariables.GetString("JAVA_SEARCH_PATH").OrGetDefault(GetDefaultJavaSearchPath);

		return new Variables(
			EnvironmentVariables.GetString("SERVER_HOST").OrThrow,
			EnvironmentVariables.GetPortNumber("SERVER_PORT").OrDefault(9401),
			javaSearchPath,
			authToken,
			authTokenFilePath,
			EnvironmentVariables.GetString("AGENT_NAME").OrDefault(string.Empty),
			(ushort) EnvironmentVariables.GetInteger("MAX_INSTANCES").OrThrow, // TODO
			RamAllocationUnits.FromString(EnvironmentVariables.GetString("MAX_MEMORY").OrThrow)
		);
	}

	private static string GetDefaultJavaSearchPath() {
		return JavaRuntimeDiscovery.GetSystemSearchPath() ?? throw new Exception("Could not automatically determine the path to Java installations on this system. Please set the JAVA_SEARCH_PATH environment variable to the folder containing Java installations.");
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
