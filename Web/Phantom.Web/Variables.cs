using Phantom.Common.Logging;
using Phantom.Utils.Runtime;

namespace Phantom.Web;

sealed record Variables(
	string ControllerHost,
	ushort ControllerPort,
	string? WebKeyToken,
	string? WebKeyFilePath,
	string WebServerHost,
	ushort WebServerPort,
	string WebBasePath
) {
	private static Variables LoadOrThrow() {
		var (webKeyToken, webKeyFilePath) = EnvironmentVariables.GetEitherString("WEB_KEY", "WEB_KEY_FILE").Require;
		
		return new Variables(
			EnvironmentVariables.GetString("CONTROLLER_HOST").Require,
			EnvironmentVariables.GetPortNumber("CONTROLLER_PORT").WithDefault(9402),
			webKeyToken,
			webKeyFilePath,
			EnvironmentVariables.GetString("WEB_SERVER_HOST").WithDefault("0.0.0.0"),
			EnvironmentVariables.GetPortNumber("WEB_SERVER_PORT").WithDefault(9400),
			EnvironmentVariables.GetString("WEB_BASE_PATH").Validate(static value => value.StartsWith('/') && value.EndsWith('/'), "Environment variable must begin and end with '/'").WithDefault("/")
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
