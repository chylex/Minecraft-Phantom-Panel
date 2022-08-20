using Phantom.Agent.Minecraft.Instance;
using Phantom.Agent.Minecraft.Java;
using Phantom.Agent.Minecraft.Launcher;
using Phantom.Agent.Minecraft.Properties;
using Phantom.Common.Data;

namespace Phantom.Agent.Services;

sealed class InstanceSessionManager {
	private const string JavaHomePath = @"C:\Users\Dan\.jdks\openjdk-17.0.1";
	private const string ServerJarPath = @"C:\Dan\Projects\Web\Minecraft-Phantom-Panel\Game\server.jar";
	private const string InstanceBasePath = @"C:\Dan\Projects\Web\Minecraft-Phantom-Panel\Game\";

	private readonly Dictionary<Guid, BaseLauncher> instanceLaunchers = new ();
	private readonly Dictionary<Guid, InstanceSession> instanceSessions = new ();

	public CreateInstanceResult Create(InstanceInfo instance) {
		var instanceFolder = Path.Combine(InstanceBasePath, instance.InstanceGuid.ToString());

		Directory.CreateDirectory(instanceFolder);

		var heapMegabytes = instance.MemoryAllocation.InMegabytes;
		var jvmProperties = new JvmProperties(
			InitialHeapMegabytes: heapMegabytes / 2,
			MaximumHeapMegabytes: heapMegabytes
		);

		var instanceProperties = new InstanceProperties(
			new JavaRuntime(JavaHomePath),
			jvmProperties,
			instanceFolder,
			ServerJarPath,
			new ServerProperties(instance.ServerPort, instance.RconPort)
		);

		VanillaLauncher launcher = new VanillaLauncher(instanceProperties);
		instanceLaunchers.Add(instance.InstanceGuid, launcher);
		return CreateInstanceResult.Success; // TODO
	}

	public async Task<LaunchResult> Start(Guid guid) {
		if (!instanceLaunchers.TryGetValue(guid, out var launcher)) {
			return new LaunchResult.InstanceNotFound();
		}

		if (instanceSessions.ContainsKey(guid)) {
			return new LaunchResult.InstanceAlreadyRunning();
		}

		InstanceSession session;
		try {
			session = await launcher.Launch();
		} catch (Exception e) {
			return new LaunchResult.UnknownError(e);
		}
		
		instanceSessions.Add(guid, session);
		return new LaunchResult.Success(guid, session);
	}

	public abstract record LaunchResult {
		private LaunchResult() {}

		public sealed record Success(Guid InstanceGuid, InstanceSession Session) : LaunchResult;

		public sealed record UnknownError(Exception Exception) : LaunchResult;
		
		public sealed record InstanceNotFound : LaunchResult;

		public sealed record InstanceAlreadyRunning : LaunchResult;
	}

	public async Task<SendCommandResult> SendCommand(Guid guid, string command) {
		if (!instanceSessions.TryGetValue(guid, out var session)) {
			return new SendCommandResult.InstanceNotRunning();
		}

		await session.SendCommand(command);
		return new SendCommandResult.Success();
	}

	public abstract record SendCommandResult {
		private SendCommandResult() {}
		
		public sealed record Success : SendCommandResult;
		
		public sealed record InstanceNotRunning : SendCommandResult;
	}

	public async Task StopAll() {
		foreach (var session in instanceSessions.Values) {
			try {
				await session.SendCommand("stop");
			} catch (Exception) {
				session.Kill();
			}
		}

		foreach (var session in instanceSessions.Values) {
			session.WaitForExit();
		}
	}
}
