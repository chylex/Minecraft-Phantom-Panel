using Phantom.Agent.Minecraft.Instance;
using Phantom.Agent.Minecraft.Java;
using Phantom.Agent.Minecraft.Launcher;
using Phantom.Agent.Minecraft.Launcher.Types;
using Phantom.Agent.Minecraft.Properties;
using Phantom.Agent.Minecraft.Server;
using Phantom.Common.Data.Agent;
using Phantom.Common.Data.Instance;
using Phantom.Common.Data.Replies;
using Phantom.Common.Messages.ToAgent;

namespace Phantom.Agent.Services.Instances;

sealed class InstanceSessionManager : IDisposable {
	private readonly AgentInfo agentInfo;
	private readonly string basePath;

	private readonly LaunchServices launchServices;
	private readonly UsedPortTracker usedPortTracker = new ();
	private readonly Dictionary<Guid, Instance> instances = new ();
	
	private readonly CancellationTokenSource shutdownCancellationTokenSource = new ();
	private readonly CancellationToken shutdownCancellationToken;
	private readonly SemaphoreSlim semaphore = new (1, 1);

	public InstanceSessionManager(AgentInfo agentInfo, AgentFolders agentFolders, JavaRuntimeRepository javaRuntimeRepository) {
		this.agentInfo = agentInfo;
		this.basePath = agentFolders.InstancesFolderPath;
		this.launchServices = new LaunchServices(new MinecraftServerExecutables(agentFolders.ServerExecutableFolderPath), javaRuntimeRepository);
		this.shutdownCancellationToken = shutdownCancellationTokenSource.Token;
	}

	public ConfigureInstanceResult Configure(InstanceConfiguration configuration) {
		try {
			semaphore.Wait(shutdownCancellationToken);
		} catch (OperationCanceledException) {
			return ConfigureInstanceResult.AgentShuttingDown;
		}

		var instanceGuid = configuration.InstanceGuid;
		
		try {
			var otherInstances = instances.Values.Where(instance => instance.Configuration.InstanceGuid != instanceGuid).ToArray();
			if (otherInstances.Length + 1 >= agentInfo.MaxInstances) {
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
				instanceFolder,
				configuration.MinecraftVersion,
				new ServerProperties(configuration.ServerPort, configuration.RconPort)
			);

			instances[instanceGuid] = new Instance(configuration, new VanillaLauncher(properties), launchServices, usedPortTracker);
			
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
