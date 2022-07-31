using Microsoft.EntityFrameworkCore;
using Npgsql;
using Phantom.Common.Rpc;
using Phantom.Server.Application;
using Phantom.Server.Rpc;
using Phantom.Server.Services;
using Phantom.Utils.Cryptography;
using Phantom.Utils.Logging;
using Phantom.Utils.Runtime;
using WebConfiguration = Phantom.Server.Web.Configuration;
using WebLauncher = Phantom.Server.Web.Launcher;

var cancellationTokenSource = new CancellationTokenSource();

PosixSignals.RegisterCancellation(cancellationTokenSource, static () => {
	PhantomLogger.Root.InformationHeading("Stopping Phantom Panel server...");
});

try {
	const string RpcServerHost = "0.0.0.0";
	const string WebServerHost = "0.0.0.0";
	
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
	
	// TODO store and show in web ui
	ServiceConfiguration.AuthToken = TokenGenerator.Create(30);
	PhantomLogger.Root.Information("Auth token for agents: {AuthToken}", Services.AgentManager.AuthToken);
	
	string certificatePath = Path.GetFullPath("./certificates");
	var certificate = await Certificates.CreateOrLoad(certificatePath);
	if (certificate is null) {
		Environment.Exit(1);
	}

	await Task.WhenAll(
		RpcLauncher.Launch(new RpcConfiguration(PhantomLogger.Create("Rpc"), RpcServerHost, rpcServerPort, certificate, cancellationTokenSource.Token), static connection => new MessageToServerListener(connection)),
		WebLauncher.Launch(new WebConfiguration(PhantomLogger.Create("Web"), WebServerHost, webServerPort, cancellationTokenSource.Token), options => options.UseNpgsql(connectionStringBuilder.ToString()))
	);
} finally {
	cancellationTokenSource.Dispose();
	PhantomLogger.Dispose();
}
