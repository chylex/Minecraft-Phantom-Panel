using Phantom.Agent;
using Phantom.Agent.Minecraft.Java;
using Phantom.Agent.Rpc;
using Phantom.Agent.Services;
using Phantom.Agent.Services.Rpc;
using Phantom.Common.Data.Agent;
using Phantom.Common.Logging;
using Phantom.Utils.Rpc;
using Phantom.Utils.Runtime;

var cancellationTokenSource = new CancellationTokenSource();
PosixSignals.RegisterCancellation(cancellationTokenSource);

try {
	PhantomLogger.Root.InformationHeading("Initializing Phantom Panel agent...");

	var (serverHost, serverPort, javaSearchPath, authToken, authTokenFilePath, agentNameOrEmpty, maxInstances, maxMemory) = Variables.LoadOrExit();

	await foreach (var runtime in JavaRuntimeDiscovery.Scan(javaSearchPath)) {
		
	}
	
	AgentAuthToken agentAuthToken;
	try {
		agentAuthToken = authTokenFilePath == null ? new AgentAuthToken(authToken) : await AgentAuthToken.ReadFromFile(authTokenFilePath);
	} catch (Exception e) {
		PhantomLogger.Root.Fatal(e, "Error reading auth token.");
		Environment.Exit(1);
		return;
	}

	string serverPublicKeyPath = Path.GetFullPath("./secrets/agent.key");
	var serverCertificate = await CertificateFile.LoadPublicKey(serverPublicKeyPath);
	if (serverCertificate == null) {
		Environment.Exit(1);
	}

	var folders = new AgentFolders("./data", "./temp", javaSearchPath);
	if (!folders.TryCreate()) {
		Environment.Exit(1);
	}

	var agentGuid = await GuidFile.CreateOrLoad(folders.DataFolderPath);
	if (agentGuid == null) {
		Environment.Exit(1);
		return;
	}

	var agentName = string.IsNullOrEmpty(agentNameOrEmpty) ? AgentNameGenerator.GenerateFrom(agentGuid.Value) : agentNameOrEmpty;
	var agentInfo = new AgentInfo(agentGuid.Value, Version: 1, agentName, maxInstances, maxMemory);
	var agentServices = new AgentServices(agentInfo, folders);

	PhantomLogger.Root.InformationHeading("Launching Phantom Panel agent...");
	await RpcLauncher.Launch(new RpcConfiguration(PhantomLogger.Create("Rpc"), serverHost, serverPort, serverCertificate, cancellationTokenSource.Token), agentAuthToken, agentInfo, socket => new MessageListener(socket, agentServices, cancellationTokenSource));

	PhantomLogger.Root.InformationHeading("Stopping Phantom Panel agent...");
	await agentServices.Shutdown();
} catch (OperationCanceledException) {
	// Ignore.
} finally {
	cancellationTokenSource.Dispose();
	PhantomLogger.Root.Information("Bye!");
	PhantomLogger.Dispose();
}
