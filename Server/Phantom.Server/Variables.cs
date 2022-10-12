using Npgsql;
using Phantom.Common.Logging;
using Phantom.Utils.Runtime;

namespace Phantom.Server;

sealed record Variables(
	string WebServerHost,
	ushort WebServerPort,
	string WebBasePath,
	string RpcServerHost,
	ushort RpcServerPort,
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
			EnvironmentVariables.GetString("WEB_SERVER_HOST").WithDefault("0.0.0.0"),
			EnvironmentVariables.GetPortNumber("WEB_SERVER_PORT").WithDefault(9400),
			EnvironmentVariables.GetString("WEB_BASE_PATH").Validate(static value => value.StartsWith('/') && value.EndsWith('/'), "Environment variable must begin and end with '/'").WithDefault("/"),
			EnvironmentVariables.GetString("RPC_SERVER_HOST").WithDefault("0.0.0.0"),
			EnvironmentVariables.GetPortNumber("RPC_SERVER_PORT").WithDefault(9401),
			connectionStringBuilder.ToString()
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
