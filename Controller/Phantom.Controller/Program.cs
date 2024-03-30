using System.Reflection;
using NetMQ;
using Phantom.Common.Messages.Agent;
using Phantom.Common.Messages.Web;
using Phantom.Controller;
using Phantom.Controller.Database.Postgres;
using Phantom.Controller.Services;
using Phantom.Utils.IO;
using Phantom.Utils.Logging;
using Phantom.Utils.Rpc;
using Phantom.Utils.Rpc.Runtime;
using Phantom.Utils.Runtime;
using Phantom.Utils.Tasks;

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
	
	var (agentRpcServerHost, agentRpcServerPort, webRpcServerHost, webRpcServerPort, sqlConnectionString) = Variables.LoadOrStop();

	string secretsPath = Path.GetFullPath("./secrets");
	CreateFolderOrStop(secretsPath, Chmod.URWX_GRX);
	
	var agentKeyDataResult = await new ConnectionKeyFiles.Agent().CreateOrLoad(secretsPath);
	if (agentKeyDataResult is not {} agentKeyData) {
		return 1;
	}
	
	var webKeyDataResult = await new ConnectionKeyFiles.Web().CreateOrLoad(secretsPath);
	if (webKeyDataResult is not {} webKeyData) {
		return 1;
	}
	
	PhantomLogger.Root.InformationHeading("Launching Phantom Panel server...");
	
	var dbContextFactory = new ApplicationDbContextFactory(sqlConnectionString);

	using var controllerServices = new ControllerServices(dbContextFactory, agentKeyData.AuthToken, webKeyData.AuthToken, shutdownCancellationToken);
	await controllerServices.Initialize();

	static RpcConfiguration ConfigureRpc(string serviceName, string host, ushort port, ConnectionKeyData connectionKey) {
		return new RpcConfiguration(serviceName, host, port, connectionKey.Certificate);
	}

	var rpcTaskManager = new TaskManager(PhantomLogger.Create<TaskManager>("Rpc"));
	try {
		await Task.WhenAll(
			RpcServerRuntime.Launch(ConfigureRpc("Agent", agentRpcServerHost, agentRpcServerPort, agentKeyData), AgentMessageRegistries.Definitions, controllerServices.AgentRegistrationHandler, controllerServices.ActorSystem, shutdownCancellationToken),
			RpcServerRuntime.Launch(ConfigureRpc("Web", webRpcServerHost, webRpcServerPort, webKeyData), WebMessageRegistries.Definitions, controllerServices.WebRegistrationHandler, controllerServices.ActorSystem, shutdownCancellationToken)
		);
	} finally {
		await rpcTaskManager.Stop();
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
