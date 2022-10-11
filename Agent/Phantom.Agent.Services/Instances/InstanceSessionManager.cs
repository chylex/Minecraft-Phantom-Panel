using Phantom.Agent.Minecraft.Instance;
using Phantom.Agent.Minecraft.Java;
using Phantom.Agent.Minecraft.Launcher;
using Phantom.Agent.Minecraft.Launcher.Types;
using Phantom.Agent.Minecraft.Properties;
using Phantom.Agent.Minecraft.Server;
using Phantom.Common.Data;
using Phantom.Common.Data.Agent;
using Phantom.Common.Data.Instance;
using Phantom.Common.Data.Minecraft;
using Phantom.Common.Data.Replies;
using Phantom.Common.Logging;
using Phantom.Utils.Threading;
using Serilog;

namespace Phantom.Agent.Services.Instances;

sealed class InstanceSessionManager : IDisposable {
	private static readonly ILogger Logger = PhantomLogger.Create<InstanceSessionManager>();
	
	private readonly AgentInfo agentInfo;
	private readonly string basePath;

	private readonly MinecraftServerExecutables minecraftServerExecutables;
	private readonly LaunchServices launchServices;
	private readonly PortManager portManager;
	private readonly Dictionary<Guid, Instance> instances = new ();

	private readonly CancellationTokenSource shutdownCancellationTokenSource = new ();
	private readonly CancellationToken shutdownCancellationToken;
	private readonly SemaphoreSlim semaphore = new (1, 1);

	public InstanceSessionManager(AgentInfo agentInfo, AgentFolders agentFolders, JavaRuntimeRepository javaRuntimeRepository, TaskManager taskManager) {
		this.agentInfo = agentInfo;
		this.basePath = agentFolders.InstancesFolderPath;
		this.minecraftServerExecutables = new MinecraftServerExecutables(agentFolders.ServerExecutableFolderPath);
		this.launchServices = new LaunchServices(taskManager, minecraftServerExecutables, javaRuntimeRepository);
		this.portManager = new PortManager(agentInfo.AllowedServerPorts, agentInfo.AllowedRconPorts);
		this.shutdownCancellationToken = shutdownCancellationTokenSource.Token;
	}

	public async Task<ConfigureInstanceResult> Configure(InstanceConfiguration configuration) {
		try {
			await semaphore.WaitAsync(shutdownCancellationToken);
		} catch (OperationCanceledException) {
			return ConfigureInstanceResult.AgentShuttingDown;
		}

		var instanceGuid = configuration.InstanceGuid;

		try {
			var otherInstances = instances.Values.Where(inst => inst.Configuration.InstanceGuid != instanceGuid).ToArray();
			if (otherInstances.Length + 1 > agentInfo.MaxInstances) {
				return ConfigureInstanceResult.InstanceLimitExceeded;
			}

			var availableMemory = agentInfo.MaxMemory - otherInstances.Aggregate(RamAllocationUnits.Zero, static (total, instance) => total + instance.Configuration.MemoryAllocation);
			if (availableMemory < configuration.MemoryAllocation) {
				return ConfigureInstanceResult.MemoryLimitExceeded;
			}

			var heapMegabytes = configuration.MemoryAllocation.InMegabytes;
			var jvmProperties = new JvmProperties(
				InitialHeapMegabytes: heapMegabytes / 2,
				MaximumHeapMegabytes: heapMegabytes
			);

			var instanceFolder = Path.Combine(basePath, instanceGuid.ToString());
			Directory.CreateDirectory(instanceFolder);

			var properties = new InstanceProperties(
				configuration.JavaRuntimeGuid,
				jvmProperties,
				configuration.JvmArguments,
				instanceFolder,
				configuration.MinecraftVersion,
				new ServerProperties(configuration.ServerPort, configuration.RconPort)
			);

			BaseLauncher launcher = new VanillaLauncher(properties);

			if (instances.TryGetValue(instanceGuid, out var instance)) {
				await instance.Reconfigure(configuration, launcher, shutdownCancellationToken);
				Logger.Information("Reconfigured instance \"{Name}\" (GUID {Guid}).", configuration.InstanceName, configuration.InstanceGuid);
			}
			else {
				instances[instanceGuid] = instance = await Instance.Create(configuration, launcher, launchServices, portManager);
				Logger.Information("Created instance \"{Name}\" (GUID {Guid}).", configuration.InstanceName, configuration.InstanceGuid);
			}

			if (configuration.LaunchAutomatically) {
				await instance.Launch(shutdownCancellationToken);
			}

			return ConfigureInstanceResult.Success;
		} finally {
			semaphore.Release();
		}
	}

	public async Task<LaunchInstanceResult> Launch(Guid instanceGuid) {
		try {
			await semaphore.WaitAsync(shutdownCancellationToken);
		} catch (OperationCanceledException) {
			return LaunchInstanceResult.AgentShuttingDown;
		}

		try {
			if (!instances.TryGetValue(instanceGuid, out var instance)) {
				return LaunchInstanceResult.InstanceDoesNotExist;
			}
			else {
				return await instance.Launch(shutdownCancellationToken);
			}
		} finally {
			semaphore.Release();
		}
	}

	public async Task<StopInstanceResult> Stop(Guid instanceGuid, MinecraftStopStrategy stopStrategy) {
		try {
			await semaphore.WaitAsync(shutdownCancellationToken);
		} catch (OperationCanceledException) {
			return StopInstanceResult.AgentShuttingDown;
		}

		try {
			if (!instances.TryGetValue(instanceGuid, out var instance)) {
				return StopInstanceResult.InstanceDoesNotExist;
			}
			else {
				return await instance.Stop(stopStrategy);
			}
		} finally {
			semaphore.Release();
		}
	}

	public async Task<SendCommandToInstanceResult> SendCommand(Guid instanceGuid, string command) {
		try {
			await semaphore.WaitAsync(shutdownCancellationToken);
		} catch (OperationCanceledException) {
			return SendCommandToInstanceResult.AgentShuttingDown;
		}

		try {
			if (!instances.TryGetValue(instanceGuid, out var instance)) {
				return SendCommandToInstanceResult.InstanceDoesNotExist;
			}

			if (!await instance.SendCommand(command, shutdownCancellationToken)) {
				return SendCommandToInstanceResult.UnknownError;
			}

			return SendCommandToInstanceResult.Success;
		} finally {
			semaphore.Release();
		}
	}

	public async Task StopAll() {
		shutdownCancellationTokenSource.Cancel();

		await semaphore.WaitAsync(CancellationToken.None);
		try {
			await Task.WhenAll(instances.Values.Select(static instance => instance.StopAndWait(TimeSpan.FromSeconds(30))));
			instances.Clear();
		} finally {
			semaphore.Release();
		}
	}

	public void Dispose() {
		minecraftServerExecutables.Dispose();
		shutdownCancellationTokenSource.Dispose();
		semaphore.Dispose();
	}
}
