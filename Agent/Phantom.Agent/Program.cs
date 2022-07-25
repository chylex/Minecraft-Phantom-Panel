using Phantom.Agent;
using Phantom.Agent.Commands;
using Phantom.Utils.Logging;

try {
	PhantomLogger.Root.InformationHeading("Launching Phantom Agent...");

	AgentServices agent = new AgentServices();
	agent.CommandListeners.Add(new TestCommandListener());

	PhantomLogger.Root.InformationHeading("Console interface ready!");

	while (Console.ReadLine() is {} line) {
		var command = line.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
		if (command.Length == 0) {
			continue;
		}

		try {
			if (command[0] == "create-instance" && command.Length == 2 && ushort.TryParse(command[1], out ushort serverPort)) {
				var rconPort = (ushort) (serverPort + 1);
				agent.CommandQueue.Add(new CreateInstanceCommand(serverPort, rconPort));
			}
			else if (command[0] == "start-instance" && command.Length == 2 && Guid.TryParse(command[1], out var guid)) {
				agent.CommandQueue.Add(new StartInstanceCommand(guid));
			}
			else if (command[0] == "send-command" && command.Length >= 3 && Guid.TryParse(command[1], out var guid2)) {
				agent.CommandQueue.Add(new SendCommandToInstanceCommand(guid2, string.Join(' ', command.Skip(2))));
			}
			else if (command[0] == "shutdown") {
				await agent.Shutdown();
				break;
			}
			else {
				Console.WriteLine("Invalid command.");
			}
		} catch (Exception e) {
			Console.WriteLine("Error: " + e.Message);
		}
	}
} finally {
	PhantomLogger.Dispose();
}
