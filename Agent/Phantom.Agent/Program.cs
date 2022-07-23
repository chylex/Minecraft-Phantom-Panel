using Phantom.Agent;
using Phantom.Utils.Terminal;

Terminal.PrintDelimiter();
Terminal.PrintLine("Launching Phantom Agent...");
Terminal.PrintDelimiter();

InstanceManager instanceManager = new ();

while (Console.ReadLine() is {} line) {
	var command = line.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
	if (command.Length == 0) {
		continue;
	}

	try {
		if (command[0] == "create-instance" && command.Length == 2 && ushort.TryParse(command[1], out ushort port)) {
			Guid guid = instanceManager.Create(port);
			Terminal.PrintLine("Created instance: " + guid);
		}
		else if (command[0] == "start-instance" && command.Length == 2 && Guid.TryParse(command[1], out var guid)) {
			var session = instanceManager.Launch(guid);
			var outputPrefix = "[" + guid + "] ";
			session.AddOutputListener((_, outputLine) => Terminal.PrintLine(outputPrefix + outputLine), uint.MaxValue);
		}
		else if (command[0] == "send-command" && command.Length >= 3 && Guid.TryParse(command[1], out var guid2)) {
			instanceManager.SendCommand(guid2, string.Join(' ', command.Skip(2)));
		}
		else if (command[0] == "stop-all") {
			instanceManager.StopAll();
			break;
		}
		else {
			Terminal.PrintLine("Invalid command.");
		}
	} catch (Exception e) {
		Terminal.PrintLine("Error: " + e.Message);
	}
}
