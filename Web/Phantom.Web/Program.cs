using System.Reflection;
using NetMQ;
using Phantom.Common.Messages.Web;
using Phantom.Common.Messages.Web.ToController;
using Phantom.Utils.Actor;
using Phantom.Utils.Cryptography;
using Phantom.Utils.IO;
using Phantom.Utils.Logging;
using Phantom.Utils.Rpc;
using Phantom.Utils.Rpc.Sockets;
using Phantom.Utils.Runtime;
using Phantom.Web;
using Phantom.Web.Services;
using Phantom.Web.Services.Rpc;

var shutdownCancellationTokenSource = new CancellationTokenSource();
var shutdownCancellationToken = shutdownCancellationTokenSource.Token;

ProgramCulture.UseInvariantCulture();

PosixSignals.RegisterCancellation(shutdownCancellationTokenSource, static () => {
	PhantomLogger.Root.InformationHeading("Stopping Phantom Panel web...");
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

	PhantomLogger.Root.InformationHeading("Initializing Phantom Panel web...");
	PhantomLogger.Root.Information("Web version: {Version}", fullVersion);

	var (controllerHost, controllerPort, webKeyToken, webKeyFilePath, webServerHost, webServerPort, webBasePath) = Variables.LoadOrStop();

	var webKey = await WebKey.Load(webKeyToken, webKeyFilePath);
	if (webKey == null) {
		return 1;
	}
	
	string dataProtectionKeysPath = Path.GetFullPath("./keys");
	CreateFolderOrStop(dataProtectionKeysPath, Chmod.URWX);

	var (controllerCertificate, webToken) = webKey.Value;
	
	var administratorToken = TokenGenerator.Create(60);
	var applicationProperties = new ApplicationProperties(fullVersion, TokenGenerator.GetBytesOrThrow(administratorToken));
	
	var rpcConfiguration = new RpcConfiguration("Web", controllerHost, controllerPort, controllerCertificate);
	var rpcSocket = RpcClientSocket.Connect(rpcConfiguration, WebMessageRegistries.Definitions, new RegisterWebMessage(webToken));

	var webConfiguration = new WebLauncher.Configuration(PhantomLogger.Create("Web"), webServerHost, webServerPort, webBasePath, dataProtectionKeysPath, shutdownCancellationToken);
	var webApplication = WebLauncher.CreateApplication(webConfiguration, applicationProperties, rpcSocket.Connection);

	using var actorSystem = ActorSystemFactory.Create("Web");
	
	ControllerMessageHandlerFactory messageHandlerFactory;
	await using (var scope = webApplication.Services.CreateAsyncScope()) {
		messageHandlerFactory = scope.ServiceProvider.GetRequiredService<ControllerMessageHandlerFactory>();
	}

	var rpcDisconnectSemaphore = new SemaphoreSlim(0, 1);
	var rpcTask = RpcClientRuntime.Launch(rpcSocket, messageHandlerFactory.Create(actorSystem), rpcDisconnectSemaphore, shutdownCancellationToken);
	try {
		PhantomLogger.Root.Information("Registering with the controller...");
		if (await messageHandlerFactory.RegisterSuccessWaiter) {
			PhantomLogger.Root.Information("Successfully registered with the controller.");
		}
		else {
			PhantomLogger.Root.Fatal("Failed to register with the controller.");
			return 1;
		}
		
		PhantomLogger.Root.InformationHeading("Launching Phantom Panel web...");
		PhantomLogger.Root.Information("Your administrator token is: {AdministratorToken}", administratorToken);
		PhantomLogger.Root.Information("For administrator setup, visit: {HttpUrl}{SetupPath}", webConfiguration.HttpUrl, webConfiguration.BasePath + "setup");
		
		await WebLauncher.Launch(webConfiguration, webApplication);
	} finally {
		shutdownCancellationTokenSource.Cancel();
		
		rpcDisconnectSemaphore.Release();
		await rpcTask;
		rpcDisconnectSemaphore.Dispose();
		
		NetMQConfig.Cleanup();
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
	shutdownCancellationTokenSource.Dispose();
	
	PhantomLogger.Root.Information("Bye!");
	PhantomLogger.Dispose();
}
