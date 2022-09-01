using Phantom.Agent.Minecraft.Instance;
using Phantom.Agent.Minecraft.Java;
using Phantom.Agent.Minecraft.Launcher;
using Phantom.Agent.Minecraft.Properties;
using Phantom.Agent.Minecraft.Server;
using Phantom.Common.Data.Agent;
using Phantom.Common.Data.Instance;
using Phantom.Common.Data.Replies;
using Phantom.Common.Messages.ToAgent;

namespace Phantom.Agent.Services.Instances;

sealed class InstanceSessionManager : IDisposable {
	private const string JavaHomePath = @"C:\Users\Dan\.jdks\openjdk-17.0.1";

	private readonly AgentInfo agentInfo;
	private readonly string basePath;

	private readonly MinecraftServerExecutables serverExecutables;
	private readonly Dictionary<Guid, Instance> instances = new ();
	private readonly HashSet<ushort> usedPorts = new ();

	private readonly CancellationTokenSource shutdownCancellationTokenSource = new ();
	private readonly CancellationToken shutdownCancellationToken;
	private readonly SemaphoreSlim semaphore = new (1, 1);

	public InstanceSessionManager(AgentInfo agentInfo, AgentFolders agentFolders) {
		this.agentInfo = agentInfo;
		this.basePath = agentFolders.InstancesFolderPath;
		this.serverExecutables = new MinecraftServerExecutables(agentFolders.ServerExecutableFolderPath);
		this.shutdownCancellationToken = shutdownCancellationTokenSource.Token;
	}

	public CreateInstanceResult Create(InstanceInfo info) {
		try {
			semaphore.Wait(shutdownCancellationToken);
		} catch (OperationCanceledException) {
			return CreateInstanceResult.AgentShuttingDown;
		}

		try {
			if (instances.TryGetValue(info.InstanceGuid, out _)) {
				return CreateInstanceResult.InstanceAlreadyExists;
			}

			if (usedPorts.Contains(info.ServerPort)) {
				return CreateInstanceResult.ServerPortInUse;
			}

			if (usedPorts.Contains(info.RconPort)) {
				return CreateInstanceResult.RconPortInUse;
			}

			if (instances.Values.Count + 1 >= agentInfo.MaxInstances) {
				return CreateInstanceResult.InstanceLimitExceeded;
			}

			var availableMemory = agentInfo.MaxMemory - instances.Values.Aggregate(RamAllocationUnits.Zero, static (total, instance) => total + instance.Info.MemoryAllocation);
			if (availableMemory < info.MemoryAllocation) {
				return CreateInstanceResult.MemoryLimitExceeded;
			}

			var heapMegabytes = info.MemoryAllocation.InMegabytes;
			var jvmProperties = new JvmProperties(
				InitialHeapMegabytes: heapMegabytes / 2,
				MaximumHeapMegabytes: heapMegabytes
			);

			var instanceFolder = Path.Combine(basePath, info.InstanceGuid.ToString());
			Directory.CreateDirectory(instanceFolder);

			var properties = new InstanceProperties(
				new JavaRuntime(JavaHomePath),
				jvmProperties,
				instanceFolder,
				info.MinecraftVersion,
				new ServerProperties(info.ServerPort, info.RconPort)
			);

			VanillaLauncher launcher = new VanillaLauncher(serverExecutables, properties);

			instances.Add(info.InstanceGuid, new Instance(info, launcher));
			usedPorts.Add(info.ServerPort);
			usedPorts.Add(info.RconPort);

			return CreateInstanceResult.Success;
		} finally {
			semaphore.Release();
		}
	}

	public async Task<SetInstanceStateResult> Update(SetInstanceStateMessage message) {
		try {
			await semaphore.WaitAsync(shutdownCancellationToken);
		} catch (OperationCanceledException) {
			return SetInstanceStateResult.AgentShuttingDown;
		}

		try {
			if (!instances.TryGetValue(message.InstanceGuid, out var instance)) {
				return SetInstanceStateResult.InstanceDoesNotExist;
			}

			if (message.IsRunning is {} isRunning) {
				if (isRunning) {
					await instance.Launch(shutdownCancellationToken);
				}
				else {
					await instance.Stop(TimeSpan.FromMinutes(1));
				}
			}

			return SetInstanceStateResult.Success;
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
		
		// ReSharper disable once MethodSupportsCancellation
		await semaphore.WaitAsync();
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
