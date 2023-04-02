using Phantom.Agent.Minecraft.Java;
using Phantom.Common.Data;
using Phantom.Common.Logging;
using Phantom.Utils.Runtime;

namespace Phantom.Agent;

sealed record Variables(
	string ServerHost,
	ushort ServerPort,
	string JavaSearchPath,
	string? AgentKeyToken,
	string? AgentKeyFilePath,
	string AgentName,
	ushort MaxInstances,
	RamAllocationUnits MaxMemory,
	AllowedPorts AllowedServerPorts,
	AllowedPorts AllowedRconPorts,
	ushort MaxConcurrentBackupCompressionTasks
) {
	private static Variables LoadOrThrow() {
		var (agentKeyToken, agentKeyFilePath) = EnvironmentVariables.GetEitherString("AGENT_KEY", "AGENT_KEY_FILE").Require;
		var javaSearchPath = EnvironmentVariables.GetString("JAVA_SEARCH_PATH").WithDefaultGetter(GetDefaultJavaSearchPath);

		return new Variables(
			EnvironmentVariables.GetString("SERVER_HOST").Require,
			EnvironmentVariables.GetPortNumber("SERVER_PORT").WithDefault(9401),
			javaSearchPath,
			agentKeyToken,
			agentKeyFilePath,
			EnvironmentVariables.GetString("AGENT_NAME").Require,
			(ushort) EnvironmentVariables.GetInteger("MAX_INSTANCES", min: 1, max: 10000).Require,
			EnvironmentVariables.GetString("MAX_MEMORY").MapParse(RamAllocationUnits.FromString).Require,
			EnvironmentVariables.GetString("ALLOWED_SERVER_PORTS").MapParse(AllowedPorts.FromString).Require,
			EnvironmentVariables.GetString("ALLOWED_RCON_PORTS").MapParse(AllowedPorts.FromString).Require,
			(ushort) EnvironmentVariables.GetInteger("MAX_CONCURRENT_BACKUP_COMPRESSION_TASKS", min: 1, max: 10000).WithDefault(1)
		);
	}

	private static string GetDefaultJavaSearchPath() {
		return JavaRuntimeDiscovery.GetSystemSearchPath() ?? throw new Exception("Could not automatically determine the path to Java installations on this system. Please set the JAVA_SEARCH_PATH environment variable to the folder containing Java installations.");
	}

	public static Variables LoadOrStop() {
		try {
			return LoadOrThrow();
		} catch (Exception e) {
			PhantomLogger.Root.Fatal(e.Message);
			throw StopProcedureException.Instance;
		}
	}
}
