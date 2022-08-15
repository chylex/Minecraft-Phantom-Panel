using Phantom.Agent;
using Phantom.Agent.Rpc;
using Phantom.Agent.Services;
using Phantom.Agent.Services.Commands;
using Phantom.Common.Data;
using Phantom.Common.Rpc;
using Phantom.Utils.Logging;
using Phantom.Utils.Runtime;

var cancellationTokenSource = new CancellationTokenSource();

PosixSignals.RegisterCancellation(cancellationTokenSource, static () => {
	PhantomLogger.Root.InformationHeading("Stopping Phantom Panel agent...");
});

try {
	Guid agentGuid = Guid.NewGuid();

	string serverHost;
	ushort serverPort;
	string? authToken;
	string? authTokenFilePath;
	string agentName;
	ushort maxInstances;
	RamAllocationUnits maxMemory;
	try {
		serverHost = EnvironmentVariables.GetString("SERVER_HOST").OrThrow;
		serverPort = EnvironmentVariables.GetPortNumber("SERVER_PORT").OrDefault(9401);
		(authToken, authTokenFilePath) = EnvironmentVariables.GetEitherString("SERVER_AUTH_TOKEN", "SERVER_AUTH_TOKEN_FILE").OrThrow;
		agentName = EnvironmentVariables.GetString("AGENT_NAME").OrGetDefault(() => AgentNameGenerator.GenerateFrom(agentGuid));
		maxInstances = (ushort) EnvironmentVariables.GetInteger("MAX_INSTANCES").OrThrow; // TODO
		maxMemory = RamAllocationUnits.FromString(EnvironmentVariables.GetString("MAX_MEMORY").OrThrow);
	} catch (Exception e) {
		PhantomLogger.Root.Fatal(e.Message);
		Environment.Exit(1);
		return;
	}

	AgentAuthToken agentAuthToken;
	try {
		agentAuthToken = await AgentTokenSource.Read(authToken, authTokenFilePath);
	} catch (Exception e) {
		PhantomLogger.Root.Fatal(e, "Error reading auth token.");
		Environment.Exit(1);
		return;
	}

	PhantomLogger.Root.InformationHeading("Launching Phantom Panel agent...");

	string serverPublicKeyPath = Path.GetFullPath("./secrets/agent.key");
	var serverCertificate = await CertificateFiles.LoadPublicKey(serverPublicKeyPath);
	if (serverCertificate is null) {
		Environment.Exit(1);
	}

	// TODO
	AgentInfo agentInfo = new AgentInfo(agentGuid, Version: 1, agentName, maxInstances, maxMemory);

	AgentServices agent = new AgentServices();
	agent.CommandListeners.Add(new TestCommandListener());

	await RpcLauncher.Launch(new RpcConfiguration(PhantomLogger.Create("Rpc"), serverHost, serverPort, serverCertificate, cancellationTokenSource.Token), agentAuthToken, agentInfo);


	void Ignore() {
		PhantomLogger.Root.InformationHeading("Console interface ready!");

		while (Console.ReadLine() is {} line) {
			var command = line.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
			if (command.Length == 0) {
				continue;
			}

			try {
				if (command[0] == "create-instance" && command.Length == 2 && ushort.TryParse(command[1], out ushort mcServerPort)) {
					var rconPort = (ushort) (mcServerPort + 1);
					agent.CommandQueue.Add(new CreateInstanceCommand(mcServerPort, rconPort));
				}
				else if (command[0] == "start-instance" && command.Length == 2 && Guid.TryParse(command[1], out var guid)) {
					agent.CommandQueue.Add(new StartInstanceCommand(guid));
				}
				else if (command[0] == "send-command" && command.Length >= 3 && Guid.TryParse(command[1], out var guid2)) {
					agent.CommandQueue.Add(new SendCommandToInstanceCommand(guid2, string.Join(' ', command.Skip(2))));
				}
				else if (command[0] == "shutdown") {
					// await agent.Shutdown();
					break;
				}
				else {
					Console.WriteLine("Invalid command.");
				}
			} catch (Exception e) {
				Console.WriteLine("Error: " + e.Message);
			}
		}
	}
} finally {
	cancellationTokenSource.Dispose();
	PhantomLogger.Dispose();
}
