using System.Diagnostics.CodeAnalysis;
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
using Phantom.Utils.IO;
using Phantom.Utils.Runtime;
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

	private async Task<InstanceActionResult<T>> AcquireSemaphoreAndRun<T>(Func<Task<InstanceActionResult<T>>> func) {
		try {
			await semaphore.WaitAsync(shutdownCancellationToken);

			try {
				return await func();
			} finally {
				semaphore.Release();
			}
		} catch (OperationCanceledException) {
			return InstanceActionResult.General<T>(InstanceActionGeneralResult.AgentShuttingDown);
		}
	}

	[SuppressMessage("ReSharper", "ConvertIfStatementToReturnStatement")]
	private Task<InstanceActionResult<T>> AcquireSemaphoreAndRunWithInstance<T>(Guid instanceGuid, Func<Instance, Task<T>> func) {
		return AcquireSemaphoreAndRun(async () => {
			if (instances.TryGetValue(instanceGuid, out var instance)) {
				return InstanceActionResult.Concrete(await func(instance));
			}
			else {
				return InstanceActionResult.General<T>(InstanceActionGeneralResult.InstanceDoesNotExist);
			}
		});
	}

	public async Task<InstanceActionResult<ConfigureInstanceResult>> Configure(InstanceConfiguration configuration) {
		return await AcquireSemaphoreAndRun(async () => {
			var instanceGuid = configuration.InstanceGuid;
			
			var otherInstances = instances.Values.Where(inst => inst.Configuration.InstanceGuid != instanceGuid).ToArray();
			if (otherInstances.Length + 1 > agentInfo.MaxInstances) {
				return InstanceActionResult.Concrete(ConfigureInstanceResult.InstanceLimitExceeded);
			}

			var availableMemory = agentInfo.MaxMemory - otherInstances.Aggregate(RamAllocationUnits.Zero, static (total, instance) => total + instance.Configuration.MemoryAllocation);
			if (availableMemory < configuration.MemoryAllocation) {
				return InstanceActionResult.Concrete(ConfigureInstanceResult.MemoryLimitExceeded);
			}

			var heapMegabytes = configuration.MemoryAllocation.InMegabytes;
			var jvmProperties = new JvmProperties(
				InitialHeapMegabytes: heapMegabytes / 2,
				MaximumHeapMegabytes: heapMegabytes
			);

			var instanceFolder = Path.Combine(basePath, instanceGuid.ToString());
			Directories.Create(instanceFolder, Chmod.URWX_GRX);

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

			return InstanceActionResult.Concrete(ConfigureInstanceResult.Success);
		});
	}

	public Task<InstanceActionResult<LaunchInstanceResult>> Launch(Guid instanceGuid) {
		return AcquireSemaphoreAndRunWithInstance(instanceGuid, instance => instance.Launch(shutdownCancellationToken));
	}

	public Task<InstanceActionResult<StopInstanceResult>> Stop(Guid instanceGuid, MinecraftStopStrategy stopStrategy) {
		return AcquireSemaphoreAndRunWithInstance(instanceGuid, instance => instance.Stop(stopStrategy));
	}

	public Task<InstanceActionResult<SendCommandToInstanceResult>> SendCommand(Guid instanceGuid, string command) {
		return AcquireSemaphoreAndRunWithInstance(instanceGuid, async instance => await instance.SendCommand(command, shutdownCancellationToken) ? SendCommandToInstanceResult.Success : SendCommandToInstanceResult.UnknownError);
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
