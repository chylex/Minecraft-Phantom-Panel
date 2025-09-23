using System.Reflection;
using Phantom.Common.Messages.Web;
using Phantom.Utils.Actor;
using Phantom.Utils.Cryptography;
using Phantom.Utils.IO;
using Phantom.Utils.Logging;
using Phantom.Utils.Rpc.Message;
using Phantom.Utils.Rpc.Runtime.Client;
using Phantom.Utils.Runtime;
using Phantom.Utils.Threading;
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
	
	var administratorToken = TokenGenerator.Create(60);
	var applicationProperties = new ApplicationProperties(fullVersion, TokenGenerator.GetBytesOrThrow(administratorToken));
	
	var rpcClientConnectionParameters = new RpcClientConnectionParameters(
		Host: controllerHost,
		Port: controllerPort,
		DistinguishedName: "phantom-controller",
		CertificateThumbprint: webKey.Value.CertificateThumbprint,
		AuthToken: webKey.Value.AuthToken,
		Handshake: new IRpcClientHandshake.NoOp(),
		MessageQueueCapacity: 250,
		FrameQueueCapacity: 500,
		MaxConcurrentlyHandledMessages: 100
	);
	
	using var rpcClient = await RpcClient<IMessageToController, IMessageToWeb>.Connect("Controller", rpcClientConnectionParameters, WebMessageRegistries.Definitions, shutdownCancellationToken);
	if (rpcClient == null) {
		PhantomLogger.Root.Fatal("Could not connect to Phantom Controller, shutting down.");
		return 1;
	}
	
	var webConfiguration = new WebLauncher.Configuration(PhantomLogger.Create("Web"), webServerHost, webServerPort, webBasePath, dataProtectionKeysPath, shutdownCancellationToken);
	var webApplication = WebLauncher.CreateApplication(webConfiguration, applicationProperties, rpcClient.MessageSender);
	
	using var actorSystem = ActorSystemFactory.Create("Web");
	
	try {
		PhantomLogger.Root.InformationHeading("Launching Phantom Panel web...");
		PhantomLogger.Root.Information("Your administrator token is: {AdministratorToken}", administratorToken);
		PhantomLogger.Root.Information("For administrator setup, visit: {HttpUrl}{SetupPath}", webConfiguration.HttpUrl, webConfiguration.BasePath + "setup");
		
		await WebLauncher.Launch(webConfiguration, webApplication);
		
		ActorRef<IMessageToWeb> rpcMessageHandlerActor;
		await using (var scope = webApplication.Services.CreateAsyncScope()) {
			var rpcMessageHandlerInit = scope.ServiceProvider.GetRequiredService<ControllerMessageHandlerActorInitFactory>().Create();
			rpcMessageHandlerActor = actorSystem.ActorOf(ControllerMessageHandlerActor.Factory(rpcMessageHandlerInit), "ControllerMessageHandler");
		}
		
		rpcClient.StartListening(new IMessageReceiver<IMessageToWeb>.Actor(rpcMessageHandlerActor));
		
		PhantomLogger.Root.Information("Phantom Panel web is ready.");
		
		await shutdownCancellationToken.WaitHandle.WaitOneAsync();
		await webApplication.StopAsync();
	} finally {
		await rpcClient.Shutdown();
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
