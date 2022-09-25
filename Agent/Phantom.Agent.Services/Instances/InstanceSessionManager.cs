using Phantom.Agent.Minecraft.Instance;
using Phantom.Agent.Minecraft.Java;
using Phantom.Agent.Minecraft.Launcher;
using Phantom.Agent.Minecraft.Properties;
using Phantom.Agent.Minecraft.Server;
using Phantom.Agent.Services.Java;
using Phantom.Common.Data.Agent;
using Phantom.Common.Data.Instance;
using Phantom.Common.Data.Replies;
using Phantom.Common.Messages.ToAgent;

namespace Phantom.Agent.Services.Instances;

sealed class InstanceSessionManager : IDisposable {
	private readonly AgentInfo agentInfo;
	private readonly string basePath;

	private readonly MinecraftServerExecutables serverExecutables;
	private readonly UsedPortTracker usedPortTracker = new ();
	private readonly Dictionary<Guid, Instance> instances = new ();

	private readonly JavaRuntimeRepository javaRuntimeRepository;
	
	private readonly CancellationTokenSource shutdownCancellationTokenSource = new ();
	private readonly CancellationToken shutdownCancellationToken;
	private readonly SemaphoreSlim semaphore = new (1, 1);

	public InstanceSessionManager(AgentInfo agentInfo, AgentFolders agentFolders, JavaRuntimeRepository javaRuntimeRepository) {
		this.agentInfo = agentInfo;
		this.basePath = agentFolders.InstancesFolderPath;
		this.serverExecutables = new MinecraftServerExecutables(agentFolders.ServerExecutableFolderPath);
		this.javaRuntimeRepository = javaRuntimeRepository;
		this.shutdownCancellationToken = shutdownCancellationTokenSource.Token;
	}

	public ConfigureInstanceResult Configure(InstanceInfo info) {
		if (!javaRuntimeRepository.TryGetByGuid(info.JavaRuntimeGuid, out var javaRuntime)) {
			return ConfigureInstanceResult.UnknownJavaRuntime;
		}
		
		try {
			semaphore.Wait(shutdownCancellationToken);
		} catch (OperationCanceledException) {
			return ConfigureInstanceResult.AgentShuttingDown;
		}

		var instanceGuid = info.InstanceGuid;
		
		try {
			var otherInstances = instances.Values.Where(instance => instance.Info.InstanceGuid != instanceGuid).ToArray();
			if (otherInstances.Length + 1 >= agentInfo.MaxInstances) {
				return ConfigureInstanceResult.InstanceLimitExceeded;
			}

			var availableMemory = agentInfo.MaxMemory - otherInstances.Aggregate(RamAllocationUnits.Zero, static (total, instance) => total + instance.Info.MemoryAllocation);
			if (availableMemory < info.MemoryAllocation) {
				return ConfigureInstanceResult.MemoryLimitExceeded;
			}

			var heapMegabytes = info.MemoryAllocation.InMegabytes;
			var jvmProperties = new JvmProperties(
				InitialHeapMegabytes: heapMegabytes / 2,
				MaximumHeapMegabytes: heapMegabytes
			);

			var instanceFolder = Path.Combine(basePath, instanceGuid.ToString());
			Directory.CreateDirectory(instanceFolder);

			var properties = new InstanceProperties(
				javaRuntime,
				jvmProperties,
				instanceFolder,
				info.MinecraftVersion,
				new ServerProperties(info.ServerPort, info.RconPort)
			);

			instances[instanceGuid] = new Instance(info, new VanillaLauncher(serverExecutables, properties), usedPortTracker);
			
			return ConfigureInstanceResult.Success;
		} finally {
			semaphore.Release();
		}
	}

	public async Task<LaunchInstanceResult> Launch(LaunchInstanceMessage message) {
		try {
			await semaphore.WaitAsync(shutdownCancellationToken);
		} catch (OperationCanceledException) {
			return LaunchInstanceResult.AgentShuttingDown;
		}

		try {
			if (!instances.TryGetValue(message.InstanceGuid, out var instance)) {
				return LaunchInstanceResult.InstanceDoesNotExist;
			}

			return await instance.Launch(shutdownCancellationToken);
		} finally {
			semaphore.Release();
		}
	}

	public async Task<SendCommandToInstanceResult> SendCommand(SendCommandToInstanceMessage message) {
		try {
			await semaphore.WaitAsync(shutdownCancellationToken);
		} catch (OperationCanceledException) {
			return SendCommandToInstanceResult.AgentShuttingDown;
		}

		try {
			if (!instances.TryGetValue(message.InstanceGuid, out var instance)) {
				return SendCommandToInstanceResult.InstanceDoesNotExist;
			}

			if (!await instance.SendCommand(message.Command, shutdownCancellationToken)) {
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
			await Task.WhenAll(instances.Values.Select(static instance => instance.Stop(TimeSpan.FromSeconds(30))));
			instances.Clear();
		} finally {
			semaphore.Release();
		}
	}

	public void Dispose() {
		shutdownCancellationTokenSource.Dispose();
		semaphore.Dispose();
	}
}
