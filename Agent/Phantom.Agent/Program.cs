using Phantom.Agent.Minecraft.Instance;
using Phantom.Agent.Minecraft.Launcher;
using Phantom.Utils.Terminal;

Terminal.PrintDelimiter();
Terminal.PrintLine("Launching Phantom Agent...");
Terminal.PrintDelimiter();

const string JreFolder = @"C:\Users\Dan\.jdks\openjdk-17.0.1";
const string ServerJar = @"C:\Dan\Projects\Web\Minecraft-Phantom-Panel\Game\server.jar";
const string InstanceFolder = @"C:\Dan\Projects\Web\Minecraft-Phantom-Panel\Game\instance";

VanillaLauncher launcher = new VanillaLauncher(new MinecraftServerLaunchProperties {
	JreFolder = JreFolder,
	InstanceFolder = InstanceFolder,
	ServerJarPath = ServerJar,
	InitialHeapMegabytes = 512,
	MaximumHeapMegabytes = 1024
});

InstanceSession session;
try {
	session = launcher.Launch();
} catch (Exception e) {
	Terminal.PrintLine("Error launching server: " + e.Message);
	Environment.Exit(1);
	return;
}

session.AddOutputListener(static (_, line) => Terminal.PrintLine(line), uint.MaxValue);

while (Console.ReadLine() is {} line) {
	session.SendCommand(line);
	
	if (line == "stop") {
		break;
	}
}

session.WaitForExit();
