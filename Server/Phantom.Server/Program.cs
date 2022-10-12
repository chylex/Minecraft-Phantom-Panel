using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Phantom.Common.Logging;
using Phantom.Server;
using Phantom.Server.Database.Postgres;
using Phantom.Server.Rpc;
using Phantom.Server.Services;
using Phantom.Server.Services.Rpc;
using Phantom.Utils.Cryptography;
using Phantom.Utils.IO;
using Phantom.Utils.Rpc;
using Phantom.Utils.Runtime;
using Phantom.Utils.Threading;
using WebConfiguration = Phantom.Server.Web.Configuration;
using WebLauncher = Phantom.Server.Web.Launcher;

var cancellationTokenSource = new CancellationTokenSource();

PosixSignals.RegisterCancellation(cancellationTokenSource, static () => {
	PhantomLogger.Root.InformationHeading("Stopping Phantom Panel server...");
});

try {
	PhantomLogger.Root.InformationHeading("Initializing Phantom Panel server...");
	
	var (webServerHost, webServerPort, webBasePath, rpcServerHost, rpcServerPort, sqlConnectionString) = Variables.LoadOrExit();

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

	var taskManager = new TaskManager();
	var rpcConfiguration = new RpcConfiguration(PhantomLogger.Create("Rpc"), rpcServerHost, rpcServerPort, certificate, taskManager, cancellationTokenSource.Token);
	var webConfiguration = new WebConfiguration(PhantomLogger.Create("Web"), webServerHost, webServerPort, webBasePath, cancellationTokenSource.Token);

	PhantomLogger.Root.InformationHeading("Launching Phantom Panel server...");
	
	var administratorToken = TokenGenerator.Create(60);
	PhantomLogger.Root.Information("Your administrator token is: {AdministratorToken}", administratorToken);
	PhantomLogger.Root.Information("For administrator setup, visit: {HttpUrl}{SetupPath}", webConfiguration.HttpUrl, webConfiguration.BasePath + "setup");

	var serviceConfiguration = new ServiceConfiguration(TokenGenerator.GetBytesOrThrow(administratorToken), taskManager, cancellationTokenSource.Token);
	var webConfigurator = new WebConfigurator(agentToken, serviceConfiguration);
	var webApplication = await WebLauncher.CreateApplication(webConfiguration, webConfigurator, options => options.UseNpgsql(sqlConnectionString, static options => {
		options.CommandTimeout(10).MigrationsAssembly(typeof(ApplicationDbContextDesignFactory).Assembly.FullName);
	}));

	await Task.WhenAll(
		RpcLauncher.Launch(rpcConfiguration, webApplication.Services.GetRequiredService<MessageToServerListenerFactory>().CreateListener),
		WebLauncher.Launch(webConfiguration, webApplication)
	);
	
	await taskManager.Stop();
} catch (OperationCanceledException) {
	// Ignore.
} finally {
	cancellationTokenSource.Dispose();
	PhantomLogger.Root.Information("Bye!");
	PhantomLogger.Dispose();
}
