using System.Reflection;
using Phantom.Agent;
using Phantom.Agent.Minecraft.Java;
using Phantom.Agent.Services;
using Phantom.Agent.Services.Rpc;
using Phantom.Common.Data.Agent;
using Phantom.Common.Messages.Agent;
using Phantom.Common.Messages.Agent.Handshake;
using Phantom.Utils.Actor;
using Phantom.Utils.Logging;
using Phantom.Utils.Rpc.Runtime.Client;
using Phantom.Utils.Runtime;
using Phantom.Utils.Threading;

const int ProtocolVersion = 1;

var shutdownCancellationTokenSource = new CancellationTokenSource();
var shutdownCancellationToken = shutdownCancellationTokenSource.Token;

ProgramCulture.UseInvariantCulture();
ThreadPool.SetMinThreads(workerThreads: 2, completionPortThreads: 1);

PosixSignals.RegisterCancellation(shutdownCancellationTokenSource, static () => {
	PhantomLogger.Root.InformationHeading("Stopping Phantom Panel agent...");
});

try {
	var fullVersion = AssemblyAttributes.GetFullVersion(Assembly.GetExecutingAssembly());
	
	PhantomLogger.Root.InformationHeading("Initializing Phantom Panel agent...");
	PhantomLogger.Root.Information("Agent version: {Version}", fullVersion);
	
	var (controllerHost, controllerPort, javaSearchPath, agentKeyToken, agentKeyFilePath, agentName, maxInstances, maxMemory, allowedServerPorts, allowedRconPorts, maxConcurrentBackupCompressionTasks) = Variables.LoadOrStop();
	
	var agentKey = await AgentKey.Load(agentKeyToken, agentKeyFilePath);
	if (agentKey == null) {
		return 1;
	}
	
	var folders = new AgentFolders("./data", "./temp", javaSearchPath);
	if (!folders.TryCreate()) {
		return 1;
	}
	
	var agentGuid = await GuidFile.CreateOrLoad(folders.DataFolderPath);
	if (agentGuid == null) {
		return 1;
	}
	
	var agentInfo = new AgentInfo(agentGuid.Value, agentName, ProtocolVersion, fullVersion, maxInstances, maxMemory, allowedServerPorts, allowedRconPorts);
	var javaRuntimeRepository = await JavaRuntimeDiscovery.Scan(folders.JavaSearchFolderPath, shutdownCancellationToken);
	
	var agentRegistrationHandler = new AgentRegistrationHandler();
	var controllerHandshake = new ControllerHandshake(new AgentRegistration(agentInfo, javaRuntimeRepository.All), agentRegistrationHandler);
	
	var rpcClientConnectionParameters = new RpcClientConnectionParameters(
		Host: controllerHost,
		Port: controllerPort,
		DistinguishedName: "phantom-controller",
		CertificateThumbprint: agentKey.Value.CertificateThumbprint,
		AuthToken: agentKey.Value.AuthToken,
		Handshake: controllerHandshake,
		MessageQueueCapacity: 250,
		FrameQueueCapacity: 500,
		MaxConcurrentlyHandledMessages: 50
	);
	
	using var rpcClient = await RpcClient<IMessageToController, IMessageToAgent>.Connect("Controller", rpcClientConnectionParameters, AgentMessageRegistries.Registries, shutdownCancellationToken);
	if (rpcClient == null) {
		PhantomLogger.Root.Fatal("Could not connect to Phantom Controller, shutting down.");
		return 1;
	}
	
	try {
		PhantomLogger.Root.InformationHeading("Launching Phantom Panel agent...");
		
		var agentServices = new AgentServices(agentInfo, folders, new AgentServiceConfiguration(maxConcurrentBackupCompressionTasks), new ControllerConnection(rpcClient.MessageSender), javaRuntimeRepository);
		
		var rpcMessageHandlerInit = new ControllerMessageHandlerActor.Init(agentServices);
		var rpcMessageHandlerActor = agentServices.ActorSystem.ActorOf(ControllerMessageHandlerActor.Factory(rpcMessageHandlerInit), "ControllerMessageHandler");
		
		rpcClient.StartListening(new ControllerMessageReceiver(rpcMessageHandlerActor, agentRegistrationHandler));
		
		if (await agentRegistrationHandler.Start(agentServices, shutdownCancellationToken)) {
			PhantomLogger.Root.Information("Phantom Panel agent is ready.");
			await shutdownCancellationToken.WaitHandle.WaitOneAsync();
		}
		
		await agentServices.Shutdown();
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
