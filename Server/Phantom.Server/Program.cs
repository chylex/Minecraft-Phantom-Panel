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
using Phantom.Utils.Tasks;
using WebConfiguration = Phantom.Server.Web.Configuration;
using WebLauncher = Phantom.Server.Web.Launcher;

var cancellationTokenSource = new CancellationTokenSource();

PosixSignals.RegisterCancellation(cancellationTokenSource, static () => {
	PhantomLogger.Root.InformationHeading("Stopping Phantom Panel server...");
});

static void CreateFolderOrStop(string path, UnixFileMode chmod) {
	if (!Directory.Exists(path)) {
		try {
			Directories.Create(path, chmod);
		} catch (Exception e) {
			PhantomLogger.Root.Fatal(e, "Error creating folder: {FolderName}", path);
			throw StopProcedureException.Instance;
		}
	}
}

try {
	var fullVersion = AssemblyAttributes.GetFullVersion(Assembly.GetExecutingAssembly());
	
	PhantomLogger.Root.InformationHeading("Initializing Phantom Panel server...");
	PhantomLogger.Root.Information("Server version: {Version}", fullVersion);
	
	var (webServerHost, webServerPort, webBasePath, rpcServerHost, rpcServerPort, sqlConnectionString) = Variables.LoadOrStop();

	string secretsPath = Path.GetFullPath("./secrets");
	CreateFolderOrStop(secretsPath, Chmod.URWX_GRX);
	
	string webKeysPath = Path.GetFullPath("./keys");
	CreateFolderOrStop(webKeysPath, Chmod.URWX);

	var certificateData = await CertificateFiles.CreateOrLoad(secretsPath);
	if (certificateData == null) {
		return 1;
	}
	
	var (certificate, agentToken) = certificateData.Value;
	
	PhantomLogger.Root.InformationHeading("Launching Phantom Panel server...");
	
	var taskManager = new TaskManager(PhantomLogger.Create<TaskManager>("Server"));
	try {
		var rpcConfiguration = new RpcConfiguration(PhantomLogger.Create("Rpc"), PhantomLogger.Create<TaskManager>("Rpc"), rpcServerHost, rpcServerPort, certificate);
		var webConfiguration = new WebConfiguration(PhantomLogger.Create("Web"), webServerHost, webServerPort, webBasePath, webKeysPath, cancellationTokenSource.Token);

		var administratorToken = TokenGenerator.Create(60);
		PhantomLogger.Root.Information("Your administrator token is: {AdministratorToken}", administratorToken);
		PhantomLogger.Root.Information("For administrator setup, visit: {HttpUrl}{SetupPath}", webConfiguration.HttpUrl, webConfiguration.BasePath + "setup");

		var serviceConfiguration = new ServiceConfiguration(fullVersion, TokenGenerator.GetBytesOrThrow(administratorToken), cancellationTokenSource.Token);
		var webConfigurator = new WebConfigurator(serviceConfiguration, taskManager, agentToken);
		var webApplication = await WebLauncher.CreateApplication(webConfiguration, webConfigurator, options => options.UseNpgsql(sqlConnectionString, static options => {
			options.CommandTimeout(10).MigrationsAssembly(typeof(ApplicationDbContextDesignFactory).Assembly.FullName);
		}));

		await Task.WhenAll(
			RpcLauncher.Launch(rpcConfiguration, webApplication.Services.GetRequiredService<MessageToServerListenerFactory>().CreateListener, cancellationTokenSource.Token),
			WebLauncher.Launch(webConfiguration, webApplication)
		);
	} finally {
		cancellationTokenSource.Cancel();
		await taskManager.Stop();
	}

	return 0;
} catch (OperationCanceledException) {
	return 0;
} catch (StopProcedureException) {
	return 1;
} catch (Exception e) {
	PhantomLogger.Root.Fatal(e, "Caught exception in entry point.");
	return 1;
} finally {
	cancellationTokenSource.Dispose();
	PhantomLogger.Root.Information("Bye!");
	PhantomLogger.Dispose();
}
