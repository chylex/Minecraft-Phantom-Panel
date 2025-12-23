using System.Reflection;
using Phantom.Common.Messages.Agent;
using Phantom.Common.Messages.Web;
using Phantom.Controller;
using Phantom.Controller.Database.Postgres;
using Phantom.Controller.Services;
using Phantom.Controller.Services.Rpc;
using Phantom.Utils.IO;
using Phantom.Utils.Logging;
using Phantom.Utils.Rpc.Runtime.Server;
using Phantom.Utils.Runtime;
using Phantom.Utils.Tasks;
using RpcAgentServer = Phantom.Utils.Rpc.Runtime.Server.RpcServer<Phantom.Common.Messages.Agent.IMessageToController, Phantom.Common.Messages.Agent.IMessageToAgent>;
using RpcWebServer = Phantom.Utils.Rpc.Runtime.Server.RpcServer<Phantom.Common.Messages.Web.IMessageToController, Phantom.Common.Messages.Web.IMessageToWeb>;

var shutdownCancellationTokenSource = new CancellationTokenSource();
var shutdownCancellationToken = shutdownCancellationTokenSource.Token;

ProgramCulture.UseInvariantCulture();

PosixSignals.RegisterCancellation(shutdownCancellationTokenSource, static () => {
	PhantomLogger.Root.InformationHeading("Stopping Phantom Panel controller...");
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
	
	PhantomLogger.Root.InformationHeading("Initializing Phantom Panel controller...");
	PhantomLogger.Root.Information("Controller version: {Version}", fullVersion);
	
	var (agentRpcServerHost, webRpcServerHost, sqlConnectionString) = Variables.LoadOrStop();
	
	string secretsPath = Path.GetFullPath("./secrets");
	CreateFolderOrStop(secretsPath, Chmod.URWX_GRX);
	
	var agentCertificate = await new CertificateFile("agent").CreateOrLoad(secretsPath);
	if (agentCertificate == null) {
		return 1;
	}
	
	var webCertificate = await new CertificateFile("web").CreateOrLoad(secretsPath);
	if (webCertificate == null) {
		return 1;
	}
	
	var webKeyDataResult = await new AuthTokenFile.Web("web", webCertificate).CreateOrLoad(secretsPath);
	if (webKeyDataResult is not {} webKeyData) {
		return 1;
	}
	
	PhantomLogger.Root.InformationHeading("Launching Phantom Panel server...");
	
	var dbContextFactory = new ApplicationDbContextFactory(sqlConnectionString);
	
	using var controllerServices = new ControllerServices(dbContextFactory, agentCertificate.Thumbprint, shutdownCancellationToken);
	await controllerServices.Initialize();
	
	var agentConnectionParameters = new RpcServerConnectionParameters(
		EndPoint: agentRpcServerHost,
		Certificate: agentCertificate,
		PingIntervalSeconds: 10,
		MessageQueueCapacity: 50,
		FrameQueueCapacity: 100,
		MaxConcurrentlyHandledMessages: 20
	);
	
	var webConnectionParameters = new RpcServerConnectionParameters(
		EndPoint: webRpcServerHost,
		Certificate: webCertificate,
		PingIntervalSeconds: 60,
		MessageQueueCapacity: 250,
		FrameQueueCapacity: 500,
		MaxConcurrentlyHandledMessages: 100
	);
	
	var webClientAuthProvider = new WebClientAuthProvider(webKeyData.AuthToken);
	
	var rpcServerTasks = new LinkedTasks<bool>([
		new RpcAgentServer("Agent", agentConnectionParameters, AgentMessageRegistries.Registries, controllerServices.AgentAuthProvider, controllerServices.AgentHandshake, controllerServices.AgentRegistrar).Run(shutdownCancellationToken),
		new RpcWebServer("Web", webConnectionParameters, WebMessageRegistries.Registries, webClientAuthProvider, new IRpcServerClientHandshake.NoOp(), controllerServices.WebRegistrar).Run(shutdownCancellationToken),
	]);
	
	// If either RPC server crashes, stop the whole process.
	await rpcServerTasks.CancelTokenWhenAnyCompletes(shutdownCancellationTokenSource);
	
	foreach (Task<bool> rpcServerTask in await rpcServerTasks.WaitForAll()) {
		if (rpcServerTask.IsFaulted || rpcServerTask is { IsCompletedSuccessfully: true, Result: false }) {
			return 1;
		}
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
