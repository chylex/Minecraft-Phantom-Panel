using Phantom.Agent;
using Phantom.Agent.Rpc;
using Phantom.Agent.Services;
using Phantom.Agent.Services.Rpc;
using Phantom.Common.Data;
using Phantom.Common.Rpc;
using Phantom.Utils.Logging;
using Phantom.Utils.Runtime;

var cancellationTokenSource = new CancellationTokenSource();
PosixSignals.RegisterCancellation(cancellationTokenSource);

try {
	Guid agentGuid = Guid.NewGuid();
	
	string DefaultAgentName() {
		return AgentNameGenerator.GenerateFrom(agentGuid);
	}

	var (serverHost, serverPort, authToken, authTokenFilePath, agentName, maxInstances, maxMemory) = Variables.LoadOrExit(DefaultAgentName);

	AgentAuthToken agentAuthToken;
	try {
		agentAuthToken = authTokenFilePath == null ? new AgentAuthToken(authToken) : await AgentAuthToken.ReadFromFile(authTokenFilePath);
	} catch (Exception e) {
		PhantomLogger.Root.Fatal(e, "Error reading auth token.");
		Environment.Exit(1);
		return;
	}

	PhantomLogger.Root.InformationHeading("Launching Phantom Panel agent...");

	string serverPublicKeyPath = Path.GetFullPath("./secrets/agent.key");
	var serverCertificate = await CertificateFiles.LoadPublicKey(serverPublicKeyPath);
	if (serverCertificate == null) {
		Environment.Exit(1);
	}

	var agentInfo = new AgentInfo(agentGuid, Version: 1, agentName, maxInstances, maxMemory);
	var agentServices = new AgentServices();

	await RpcLauncher.Launch(new RpcConfiguration(PhantomLogger.Create("Rpc"), serverHost, serverPort, serverCertificate, cancellationTokenSource.Token), agentAuthToken, agentInfo, socket => new MessageListener(socket, cancellationTokenSource));

	PhantomLogger.Root.InformationHeading("Stopping Phantom Panel agent...");
	await agentServices.Shutdown();
} finally {
	cancellationTokenSource.Dispose();
	PhantomLogger.Dispose();
}
