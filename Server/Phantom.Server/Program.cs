using System.Reflection;
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
using WebConfiguration = Phantom.Server.Web.Configuration;
using WebLauncher = Phantom.Server.Web.Launcher;

var cancellationTokenSource = new CancellationTokenSource();
var taskManager = new TaskManager();

PosixSignals.RegisterCancellation(cancellationTokenSource, static () => {
	PhantomLogger.Root.InformationHeading("Stopping Phantom Panel server...");
});

static void CreateFolderOrExit(string path, UnixFileMode chmod) {
	if (!Directory.Exists(path)) {
		try {
			Directories.Create(path, chmod);
		} catch (Exception e) {
			PhantomLogger.Root.Fatal(e, "Error creating folder: {FolderName}", path);
			Environment.Exit(1);
		}
	}
}

try {
	var fullVersion = AssemblyAttributes.GetFullVersion(Assembly.GetExecutingAssembly());
	
	PhantomLogger.Root.InformationHeading("Initializing Phantom Panel server...");
	PhantomLogger.Root.Information("Server version: {Version}", fullVersion);
	
	var (webServerHost, webServerPort, webBasePath, rpcServerHost, rpcServerPort, sqlConnectionString) = Variables.LoadOrExit();

	string secretsPath = Path.GetFullPath("./secrets");
	CreateFolderOrExit(secretsPath, Chmod.URWX_GRX);
	
	string webKeysPath = Path.GetFullPath("./keys");
	CreateFolderOrExit(webKeysPath, Chmod.URWX);

	var certificateData = await CertificateFiles.CreateOrLoad(secretsPath);
	if (certificateData == null) {
		Environment.Exit(1);
	}
	
	var (certificate, agentToken) = certificateData.Value;
	
	var rpcConfiguration = new RpcConfiguration(PhantomLogger.Create("Rpc"), rpcServerHost, rpcServerPort, certificate, cancellationTokenSource.Token);
	var webConfiguration = new WebConfiguration(PhantomLogger.Create("Web"), webServerHost, webServerPort, webBasePath, webKeysPath, cancellationTokenSource.Token);

	PhantomLogger.Root.InformationHeading("Launching Phantom Panel server...");
	
	var administratorToken = TokenGenerator.Create(60);
	PhantomLogger.Root.Information("Your administrator token is: {AdministratorToken}", administratorToken);
	PhantomLogger.Root.Information("For administrator setup, visit: {HttpUrl}{SetupPath}", webConfiguration.HttpUrl, webConfiguration.BasePath + "setup");

	var serviceConfiguration = new ServiceConfiguration(fullVersion, TokenGenerator.GetBytesOrThrow(administratorToken), cancellationTokenSource.Token);
	var webConfigurator = new WebConfigurator(serviceConfiguration, taskManager, agentToken);
	var webApplication = await WebLauncher.CreateApplication(webConfiguration, webConfigurator, options => options.UseNpgsql(sqlConnectionString, static options => {
		options.CommandTimeout(10).MigrationsAssembly(typeof(ApplicationDbContextDesignFactory).Assembly.FullName);
	}));

	await Task.WhenAll(
		RpcLauncher.Launch(rpcConfiguration, webApplication.Services.GetRequiredService<MessageToServerListenerFactory>().CreateListener),
		WebLauncher.Launch(webConfiguration, webApplication)
	);
} catch (OperationCanceledException) {
	// Ignore.
} finally {
	cancellationTokenSource.Cancel();
	
	PhantomLogger.Root.Information("Stopping task manager...");
	await taskManager.Stop();
	
	cancellationTokenSource.Dispose();
	PhantomLogger.Root.Information("Bye!");
	PhantomLogger.Dispose();
}
