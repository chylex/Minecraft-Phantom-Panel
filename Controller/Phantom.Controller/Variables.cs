using System.Net;
using Npgsql;
using Phantom.Utils.Logging;
using Phantom.Utils.Runtime;

namespace Phantom.Controller;

sealed record Variables(
	EndPoint AgentRpcServerHost,
	EndPoint WebRpcServerHost,
	string SqlConnectionString
) {
	private static Variables LoadOrThrow() {
		var connectionStringBuilder = new NpgsqlConnectionStringBuilder {
			Host = EnvironmentVariables.GetString("PG_HOST").Require,
			Port = EnvironmentVariables.GetPortNumber("PG_PORT").Require,
			Username = EnvironmentVariables.GetString("PG_USER").Require,
			Password = EnvironmentVariables.GetString("PG_PASS").Require,
			Database = EnvironmentVariables.GetString("PG_DATABASE").Require,
		};
		
		EndPoint agentRpcServerHost = new IPEndPoint(
			EnvironmentVariables.GetIpAddress("AGENT_RPC_SERVER_HOST").WithDefault(IPAddress.Any),
			EnvironmentVariables.GetPortNumber("AGENT_RPC_SERVER_PORT").WithDefault(9401)
		);
		
		EndPoint webRpcServerHost = new IPEndPoint(
			EnvironmentVariables.GetIpAddress("WEB_RPC_SERVER_HOST").WithDefault(IPAddress.Any),
			EnvironmentVariables.GetPortNumber("WEB_RPC_SERVER_PORT").WithDefault(9402)
		);
		
		return new Variables(
			agentRpcServerHost,
			webRpcServerHost,
			connectionStringBuilder.ToString()
		);
	}
	
	public static Variables LoadOrStop() {
		try {
			return LoadOrThrow();
		} catch (Exception e) {
			PhantomLogger.Root.Fatal("{Error}", e.Message);
			throw StopProcedureException.Instance;
		}
	}
}
