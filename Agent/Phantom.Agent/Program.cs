﻿using Phantom.Agent;
using Phantom.Agent.Rpc;
using Phantom.Agent.Services;
using Phantom.Agent.Services.Rpc;
using Phantom.Common.Data.Agent;
using Phantom.Common.Logging;
using Phantom.Utils.Rpc;
using Phantom.Utils.Runtime;

const int AgentVersion = 1;

var cancellationTokenSource = new CancellationTokenSource();

PosixSignals.RegisterCancellation(cancellationTokenSource, static () => {
	PhantomLogger.Root.InformationHeading("Stopping Phantom Panel agent...");
});

try {
	PhantomLogger.Root.InformationHeading("Initializing Phantom Panel agent...");

	var (serverHost, serverPort, authToken, authTokenFilePath, agentName) = Variables.LoadOrExit();
	
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

	var folders = new AgentFolders("./data", "./temp");
	if (!folders.TryCreate()) {
		Environment.Exit(1);
	}

	var agentGuid = await GuidFile.CreateOrLoad(folders.DataFolderPath);
	if (agentGuid == null) {
		Environment.Exit(1);
		return;
	}

	var agentInfo = new AgentInfo(agentGuid.Value, agentName, AgentVersion);

	PhantomLogger.Root.InformationHeading("Launching Phantom Panel agent...");
	
	await RpcLauncher.Launch(new RpcConfiguration(PhantomLogger.Create("Rpc"), serverHost, serverPort, serverCertificate, cancellationTokenSource.Token), agentAuthToken, agentInfo, socket => new MessageListener(socket, cancellationTokenSource));
} catch (OperationCanceledException) {
	// Ignore.
} finally {
	cancellationTokenSource.Dispose();
	PhantomLogger.Root.Information("Bye!");
	PhantomLogger.Dispose();
}