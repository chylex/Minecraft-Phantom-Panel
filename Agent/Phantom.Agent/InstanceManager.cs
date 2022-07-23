using Phantom.Agent.Minecraft.Instance;
using Phantom.Agent.Minecraft.Java;
using Phantom.Agent.Minecraft.Launcher;

namespace Phantom.Agent;

sealed class InstanceManager {
	private const string JavaHomePath = @"C:\Users\Dan\.jdks\openjdk-17.0.1";
	private const string ServerJarPath = @"C:\Dan\Projects\Web\Minecraft-Phantom-Panel\Game\server.jar";
	private const string InstanceBasePath = @"C:\Dan\Projects\Web\Minecraft-Phantom-Panel\Game\";

	private readonly Dictionary<Guid, MinecraftServerLauncher> instanceLaunchers = new ();
	private readonly Dictionary<Guid, InstanceSession> instanceSessions = new ();

	public Guid Create(ushort port) {
		var sessionId = Guid.NewGuid();
		var instanceFolder = Path.Combine(InstanceBasePath, sessionId.ToString());
		
		Directory.CreateDirectory(instanceFolder);
		
		VanillaLauncher launcher = new VanillaLauncher(new MinecraftServerLaunchProperties {
			JavaRuntime = new JavaRuntime(JavaHomePath),
			InstanceFolder = instanceFolder,
			ServerJarPath = ServerJarPath,
			InitialHeapMegabytes = 512,
			MaximumHeapMegabytes = 1024,
			Port = port
		});

		instanceLaunchers.Add(sessionId, launcher);
		return sessionId;
	}

	public InstanceSession Launch(Guid guid) {
		if (!instanceLaunchers.TryGetValue(guid, out var launcher)) {
			throw new ArgumentException("Instance not found.", nameof(guid));
		}
		
		if (instanceSessions.ContainsKey(guid)) {
			throw new ArgumentException("Instance is already running.", nameof(guid));
		}

		var session = launcher.Launch();
		instanceSessions.Add(guid, session);
		return session;
	}

	public void SendCommand(Guid guid, string command) {
		if (!instanceSessions.TryGetValue(guid, out var session)) {
			throw new ArgumentException("Instance is not running.", nameof(guid));
		}
		
		session.SendCommand(command);
	}

	public void StopAll() {
		foreach (var session in instanceSessions.Values) {
			session.SendCommand("stop");
		}

		foreach (var session in instanceSessions.Values) {
			session.WaitForExit();
		}
	}
}
