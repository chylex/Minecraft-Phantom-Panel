using Microsoft.EntityFrameworkCore;
using Npgsql;
using Phantom.Common.Rpc;
using Phantom.Server.Application;
using Phantom.Server.Rpc;
using Phantom.Server.Services;
using Phantom.Server.Services.Rpc;
using Phantom.Utils.IO;
using Phantom.Utils.Logging;
using Phantom.Utils.Runtime;
using WebConfiguration = Phantom.Server.Web.Configuration;
using WebLauncher = Phantom.Server.Web.Launcher;

var cancellationTokenSource = new CancellationTokenSource();

PosixSignals.RegisterCancellation(cancellationTokenSource, static () => {
	PhantomLogger.Root.InformationHeading("Stopping Phantom Panel server...");
});

try {
	string webServerHost = EnvironmentVariables.GetString("WEB_SERVER_HOST").OrDefault("0.0.0.0");
	string rpcServerHost = EnvironmentVariables.GetString("RPC_SERVER_HOST").OrDefault("0.0.0.0");

	ushort webServerPort = EnvironmentVariables.GetPortNumber("WEB_SERVER_PORT").OrDefault(9400);
	ushort rpcServerPort = EnvironmentVariables.GetPortNumber("RPC_SERVER_PORT").OrDefault(9401);

	var connectionStringBuilder = new NpgsqlConnectionStringBuilder();
	try {
		connectionStringBuilder.Host = EnvironmentVariables.GetString("PG_HOST").OrThrow;
		connectionStringBuilder.Port = EnvironmentVariables.GetPortNumber("PG_PORT").OrThrow;
		connectionStringBuilder.Username = EnvironmentVariables.GetString("PG_USER").OrThrow;
		connectionStringBuilder.Password = EnvironmentVariables.GetString("PG_PASS").OrThrow;
		connectionStringBuilder.Database = EnvironmentVariables.GetString("PG_DATABASE").OrThrow;
	} catch (Exception e) {
		PhantomLogger.Root.Fatal(e.Message);
		Environment.Exit(1);
	}

	PhantomLogger.Root.InformationHeading("Launching Phantom Panel server...");

	string secretsPath = Path.GetFullPath("./secrets");
	if (!Directory.Exists(secretsPath)) {
		try {
			Directories.Create(secretsPath, Chmod.URW_GR);
		} catch (Exception e) {
			PhantomLogger.Root.Fatal(e, "Error creating secrets folder.");
			Environment.Exit(1);
		}
	}

	string? agentToken = await AgentTokenFile.CreateOrLoad(secretsPath);
	if (agentToken == null) {
		Environment.Exit(1);
	}

	ServiceConfiguration.AgentToken = agentToken;

	var certificate = await CertificateFiles.CreateOrLoad(secretsPath);
	if (certificate is null) {
		Environment.Exit(1);
	}

	await Task.WhenAll(
		RpcLauncher.Launch(new RpcConfiguration(PhantomLogger.Create("Rpc"), rpcServerHost, rpcServerPort, certificate, cancellationTokenSource.Token), static connection => new MessageToServerListener(connection)),
		WebLauncher.Launch(new WebConfiguration(PhantomLogger.Create("Web"), webServerHost, webServerPort, cancellationTokenSource.Token), options => options.UseNpgsql(connectionStringBuilder.ToString()))
	);
} finally {
	cancellationTokenSource.Dispose();
	PhantomLogger.Dispose();
}
