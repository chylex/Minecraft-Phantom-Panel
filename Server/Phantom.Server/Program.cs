using Microsoft.EntityFrameworkCore;
using Phantom.Common.Logging;
using Phantom.Server;
using Phantom.Server.Rpc;
using Phantom.Server.Services;
using Phantom.Server.Services.Rpc;
using Phantom.Utils.IO;
using Phantom.Utils.Rpc;
using Phantom.Utils.Runtime;
using WebConfiguration = Phantom.Server.Web.Configuration;
using WebLauncher = Phantom.Server.Web.Launcher;

var cancellationTokenSource = new CancellationTokenSource();

PosixSignals.RegisterCancellation(cancellationTokenSource, static () => {
	PhantomLogger.Root.InformationHeading("Stopping Phantom Panel server...");
});

try {
	var (webServerHost, webServerPort, rpcServerHost, rpcServerPort, sqlConnectionString) = Variables.LoadOrExit();
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

	var agentToken = await AgentTokenFile.CreateOrLoad(secretsPath);
	if (agentToken == null) {
		Environment.Exit(1);
	}

	ServiceConfiguration.AgentToken = agentToken;
	ServiceConfiguration.CancellationToken = cancellationTokenSource.Token;

	var certificate = await CertificateFiles.CreateOrLoad(secretsPath);
	if (certificate == null) {
		Environment.Exit(1);
	}

	await Task.WhenAll(
		RpcLauncher.Launch(new RpcConfiguration(PhantomLogger.Create("Rpc"), rpcServerHost, rpcServerPort, certificate, cancellationTokenSource.Token), static connection => new MessageToServerListener(connection)),
		WebLauncher.Launch(new WebConfiguration(PhantomLogger.Create("Web"), webServerHost, webServerPort, cancellationTokenSource.Token), options => options.UseNpgsql(sqlConnectionString))
	);
	
	PhantomLogger.Root.Information("Bye!");
} finally {
	cancellationTokenSource.Dispose();
	PhantomLogger.Dispose();
}
