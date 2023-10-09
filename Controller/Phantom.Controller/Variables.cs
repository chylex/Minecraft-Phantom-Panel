using Npgsql;
using Phantom.Common.Logging;
using Phantom.Utils.Runtime;

namespace Phantom.Controller;

sealed record Variables(
	string AgentRpcServerHost,
	ushort AgentRpcServerPort,
	string WebRpcServerHost,
	ushort WebRpcServerPort,
	string SqlConnectionString
) {
	private static Variables LoadOrThrow() {
		var connectionStringBuilder = new NpgsqlConnectionStringBuilder {
			Host = EnvironmentVariables.GetString("PG_HOST").Require,
			Port = EnvironmentVariables.GetPortNumber("PG_PORT").Require,
			Username = EnvironmentVariables.GetString("PG_USER").Require,
			Password = EnvironmentVariables.GetString("PG_PASS").Require,
			Database = EnvironmentVariables.GetString("PG_DATABASE").Require
		};

		return new Variables(
			EnvironmentVariables.GetString("AGENT_RPC_SERVER_HOST").WithDefault("0.0.0.0"),
			EnvironmentVariables.GetPortNumber("AGENT_RPC_SERVER_PORT").WithDefault(9401),
			EnvironmentVariables.GetString("WEB_RPC_SERVER_HOST").WithDefault("0.0.0.0"),
			EnvironmentVariables.GetPortNumber("WEB_RPC_SERVER_PORT").WithDefault(9402),
			connectionStringBuilder.ToString()
		);
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
