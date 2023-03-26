using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Phantom.Agent.Minecraft.Instance;
using Phantom.Agent.Minecraft.Java;
using Phantom.Agent.Minecraft.Launcher;
using Phantom.Agent.Minecraft.Launcher.Types;
using Phantom.Agent.Minecraft.Properties;
using Phantom.Agent.Minecraft.Server;
using Phantom.Agent.Rpc;
using Phantom.Agent.Services.Backups;
using Phantom.Common.Data;
using Phantom.Common.Data.Agent;
using Phantom.Common.Data.Instance;
using Phantom.Common.Data.Minecraft;
using Phantom.Common.Data.Replies;
using Phantom.Common.Logging;
using Phantom.Common.Messages.ToServer;
using Phantom.Utils.IO;
using Phantom.Utils.Runtime;
using Serilog;

namespace Phantom.Agent.Services.Instances;

sealed class InstanceSessionManager : IAsyncDisposable {
	private static readonly ILogger Logger = PhantomLogger.Create<InstanceSessionManager>();

	private readonly AgentInfo agentInfo;
	private readonly string basePath;

	private readonly InstanceServices instanceServices;
	private readonly Dictionary<Guid, Instance> instances = new ();

	private readonly CancellationTokenSource shutdownCancellationTokenSource = new ();
	private readonly CancellationToken shutdownCancellationToken;
	private readonly SemaphoreSlim semaphore = new (1, 1);

	private uint instanceLoggerSequenceId = 0;

	public InstanceSessionManager(AgentInfo agentInfo, AgentFolders agentFolders, JavaRuntimeRepository javaRuntimeRepository, TaskManager taskManager, BackupManager backupManager) {
		this.agentInfo = agentInfo;
		this.basePath = agentFolders.InstancesFolderPath;
		this.shutdownCancellationToken = shutdownCancellationTokenSource.Token;

		var minecraftServerExecutables = new MinecraftServerExecutables(agentFolders.ServerExecutableFolderPath);
		var launchServices = new LaunchServices(minecraftServerExecutables, javaRuntimeRepository);
		var portManager = new PortManager(agentInfo.AllowedServerPorts, agentInfo.AllowedRconPorts);

		this.instanceServices = new InstanceServices(taskManager, portManager, backupManager, launchServices);
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

	public async Task<InstanceActionResult<ConfigureInstanceResult>> Configure(InstanceConfiguration configuration, InstanceLaunchProperties launchProperties, bool launchNow, bool alwaysReportStatus) {
		return await AcquireSemaphoreAndRun(async () => {
			var instanceGuid = configuration.InstanceGuid;
			var instanceFolder = Path.Combine(basePath, instanceGuid.ToString());
			Directories.Create(instanceFolder, Chmod.URWX_GRX);

			var heapMegabytes = configuration.MemoryAllocation.InMegabytes;
			var jvmProperties = new JvmProperties(
				InitialHeapMegabytes: heapMegabytes / 2,
				MaximumHeapMegabytes: heapMegabytes
			);

			var properties = new InstanceProperties(
				instanceGuid,
				configuration.JavaRuntimeGuid,
				jvmProperties,
				configuration.JvmArguments,
				instanceFolder,
				configuration.MinecraftVersion,
				new ServerProperties(configuration.ServerPort, configuration.RconPort),
				launchProperties
			);

			IServerLauncher launcher = configuration.MinecraftServerKind switch {
				MinecraftServerKind.Vanilla => new VanillaLauncher(properties),
				MinecraftServerKind.Fabric  => new FabricLauncher(properties),
				_                           => InvalidLauncher.Instance
			};

			if (instances.TryGetValue(instanceGuid, out var instance)) {
				await instance.Reconfigure(configuration, launcher, shutdownCancellationToken);
				Logger.Information("Reconfigured instance \"{Name}\" (GUID {Guid}).", configuration.InstanceName, configuration.InstanceGuid);

				if (alwaysReportStatus) {
					instance.ReportLastStatus();
				}
			}
			else {
				instances[instanceGuid] = instance = new Instance(GetInstanceLoggerName(instanceGuid), instanceServices, configuration, launcher);
				Logger.Information("Created instance \"{Name}\" (GUID {Guid}).", configuration.InstanceName, configuration.InstanceGuid);

				instance.ReportLastStatus();
				instance.IsRunningChanged += OnInstanceIsRunningChanged;
			}

			if (launchNow) {
				await LaunchInternal(instance);
			}

			return InstanceActionResult.Concrete(ConfigureInstanceResult.Success);
		});
	}

	private string GetInstanceLoggerName(Guid guid) {
		var prefix = guid.ToString();
		return prefix[..prefix.IndexOf('-')] + "/" + Interlocked.Increment(ref instanceLoggerSequenceId);
	}

	private ImmutableArray<Instance> GetRunningInstancesInternal() {
		return instances.Values.Where(static instance => instance.IsRunning).ToImmutableArray();
	}

	private void OnInstanceIsRunningChanged(object? sender, EventArgs e) {
		instanceServices.TaskManager.Run("Handle instance running state changed event", RefreshAgentStatus);
	}

	public async Task RefreshAgentStatus() {
		try {
			await semaphore.WaitAsync(shutdownCancellationToken);
			try {
				var runningInstances = GetRunningInstancesInternal();
				var runningInstanceCount = runningInstances.Length;
				var runningInstanceMemory = runningInstances.Aggregate(RamAllocationUnits.Zero, static (total, instance) => total + instance.Configuration.MemoryAllocation);
				await ServerMessaging.Send(new ReportAgentStatusMessage(runningInstanceCount, runningInstanceMemory));
			} finally {
				semaphore.Release();
			}
		} catch (OperationCanceledException) {
			// ignore
		}
	}

	public Task<InstanceActionResult<LaunchInstanceResult>> Launch(Guid instanceGuid) {
		return AcquireSemaphoreAndRunWithInstance(instanceGuid, LaunchInternal);
	}

	private async Task<LaunchInstanceResult> LaunchInternal(Instance instance) {
		var runningInstances = GetRunningInstancesInternal();
		if (runningInstances.Length + 1 > agentInfo.MaxInstances) {
			return LaunchInstanceResult.InstanceLimitExceeded;
		}

		var availableMemory = agentInfo.MaxMemory - runningInstances.Aggregate(RamAllocationUnits.Zero, static (total, instance) => total + instance.Configuration.MemoryAllocation);
		if (availableMemory < instance.Configuration.MemoryAllocation) {
			return LaunchInstanceResult.MemoryLimitExceeded;
		}

		return await instance.Launch(shutdownCancellationToken);
	}

	public Task<InstanceActionResult<StopInstanceResult>> Stop(Guid instanceGuid, MinecraftStopStrategy stopStrategy) {
		return AcquireSemaphoreAndRunWithInstance(instanceGuid, instance => instance.Stop(stopStrategy, shutdownCancellationToken));
	}

	public Task<InstanceActionResult<SendCommandToInstanceResult>> SendCommand(Guid instanceGuid, string command) {
		return AcquireSemaphoreAndRunWithInstance(instanceGuid, async instance => await instance.SendCommand(command, shutdownCancellationToken) ? SendCommandToInstanceResult.Success : SendCommandToInstanceResult.UnknownError);
	}

	public async ValueTask DisposeAsync() {
		Logger.Information("Stopping all instances...");
		
		shutdownCancellationTokenSource.Cancel();

		await semaphore.WaitAsync(CancellationToken.None);
		await Task.WhenAll(instances.Values.Select(static instance => instance.DisposeAsync().AsTask()));
		instances.Clear();
		
		shutdownCancellationTokenSource.Dispose();
		semaphore.Dispose();
	}
}
