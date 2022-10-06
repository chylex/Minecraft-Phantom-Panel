using Phantom.Common.Logging;
using Phantom.Utils.Runtime;

namespace Phantom.Server;

sealed record Variables(
	string WebServerHost,
	ushort WebServerPort,
	string RpcServerHost,
	ushort RpcServerPort
) {
	private static Variables LoadOrThrow() {
		return new Variables(
			EnvironmentVariables.GetString("WEB_SERVER_HOST").OrDefault("0.0.0.0"),
			EnvironmentVariables.GetPortNumber("WEB_SERVER_PORT").OrDefault(9400),
			EnvironmentVariables.GetString("RPC_SERVER_HOST").OrDefault("0.0.0.0"),
			EnvironmentVariables.GetPortNumber("RPC_SERVER_PORT").OrDefault(9401)
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
