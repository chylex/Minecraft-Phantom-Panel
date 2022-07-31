using Phantom.Agent;
using Phantom.Agent.Commands;
using Phantom.Agent.Rpc;
using Phantom.Common.Rpc;
using Phantom.Utils.Logging;
using Phantom.Utils.Runtime;

var cancellationTokenSource = new CancellationTokenSource();
	
PosixSignals.RegisterCancellation(cancellationTokenSource, static () => {
	PhantomLogger.Root.InformationHeading("Stopping Phantom Panel agent...");
});

try {
	string serverHost;
	ushort serverPort = EnvironmentVariables.GetPortNumber("SERVER_PORT").OrDefault(9401);
	try {
		serverHost = EnvironmentVariables.GetString("SERVER_HOST").OrThrow;
	} catch (Exception e) {
		PhantomLogger.Root.Fatal(e.Message);
		Environment.Exit(1);
		return;
	}

	PhantomLogger.Root.InformationHeading("Launching Phantom Panel agent...");
	
	string serverPublicKeyPath = Path.GetFullPath("./certificates/agent.key");
	var serverCertificate = await Certificates.LoadPublicKey(serverPublicKeyPath);
	if (serverCertificate is null) {
		Environment.Exit(1);
	}

	AgentServices agent = new AgentServices();
	agent.CommandListeners.Add(new TestCommandListener());
	
	await RpcLauncher.Launch(new RpcConfiguration(PhantomLogger.Create("Rpc"), serverHost, serverPort, serverCertificate, cancellationTokenSource.Token));

	
	
	
	
	
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
