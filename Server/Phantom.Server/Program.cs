using Microsoft.Extensions.DependencyInjection;
using Phantom.Common.Logging;
using Phantom.Server;
using Phantom.Server.Rpc;
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
	PhantomLogger.Root.InformationHeading("Initializing Phantom Panel server...");
	
	var (webServerHost, webServerPort, rpcServerHost, rpcServerPort) = Variables.LoadOrExit();

	string secretsPath = Path.GetFullPath("./secrets");
	if (!Directory.Exists(secretsPath)) {
		try {
			Directories.Create(secretsPath, Chmod.URWX_GRX);
		} catch (Exception e) {
			PhantomLogger.Root.Fatal(e, "Error creating secrets folder.");
			Environment.Exit(1);
		}
	}

	var agentToken = await AgentTokenFile.CreateOrLoad(secretsPath);
	if (agentToken == null) {
		Environment.Exit(1);
	}

	var certificate = await CertificateFiles.CreateOrLoad(secretsPath);
	if (certificate == null) {
		Environment.Exit(1);
	}

	var rpcConfiguration = new RpcConfiguration(PhantomLogger.Create("Rpc"), rpcServerHost, rpcServerPort, certificate, cancellationTokenSource.Token);
	var webConfiguration = new WebConfiguration(PhantomLogger.Create("Web"), webServerHost, webServerPort, cancellationTokenSource.Token);

	PhantomLogger.Root.InformationHeading("Launching Phantom Panel server...");
	
	var webConfigurator = new WebConfigurator(agentToken, cancellationTokenSource.Token);
	var webApplication = WebLauncher.CreateApplication(webConfiguration, webConfigurator);

	await Task.WhenAll(
		RpcLauncher.Launch(rpcConfiguration, webApplication.Services.GetRequiredService<MessageToServerListenerFactory>().CreateListener),
		WebLauncher.Launch(webConfiguration, webApplication)
	);
} catch (OperationCanceledException) {
	// Ignore.
} finally {
	cancellationTokenSource.Dispose();
	PhantomLogger.Root.Information("Bye!");
	PhantomLogger.Dispose();
}
