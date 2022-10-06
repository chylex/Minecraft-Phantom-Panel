using Phantom.Agent;
using Phantom.Agent.Services;
using Phantom.Common.Data.Agent;
using Phantom.Common.Logging;
using Phantom.Utils.Runtime;

var cancellationTokenSource = new CancellationTokenSource();

PosixSignals.RegisterCancellation(cancellationTokenSource, static () => {
	PhantomLogger.Root.InformationHeading("Stopping Phantom Panel agent...");
});

try {
	PhantomLogger.Root.InformationHeading("Initializing Phantom Panel agent...");

	var (authToken, authTokenFilePath) = Variables.LoadOrExit();
	
	AgentAuthToken agentAuthToken;
	try {
		agentAuthToken = authTokenFilePath == null ? new AgentAuthToken(authToken) : await AgentAuthToken.ReadFromFile(authTokenFilePath);
	} catch (Exception e) {
		PhantomLogger.Root.Fatal(e, "Error reading auth token.");
		Environment.Exit(1);
		return;
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

	PhantomLogger.Root.InformationHeading("Launching Phantom Panel agent...");
	
	PhantomLogger.Root.Information("Token: {Token}", agentAuthToken.Value);
} catch (OperationCanceledException) {
	// Ignore.
} finally {
	cancellationTokenSource.Dispose();
	PhantomLogger.Root.Information("Bye!");
	PhantomLogger.Dispose();
}
