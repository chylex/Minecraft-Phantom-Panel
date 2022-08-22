using Phantom.Agent.Minecraft.Instance;
using Phantom.Agent.Minecraft.Java;
using Phantom.Agent.Minecraft.Launcher;
using Phantom.Agent.Minecraft.Properties;
using Phantom.Common.Data;
using Phantom.Common.Messages.ToAgent;

namespace Phantom.Agent.Services.Instances;

sealed class InstanceSessionManager : IDisposable {
	private const string JavaHomePath = @"C:\Users\Dan\.jdks\openjdk-17.0.1";

	private readonly AgentInfo agentInfo;
	private readonly string basePath;

	private readonly Dictionary<Guid, Instance> instances = new ();
	private readonly HashSet<ushort> usedPorts = new ();

	private readonly CancellationTokenSource shutdownCancellationTokenSource = new ();
	private readonly SemaphoreSlim semaphore = new (1, 1);

	public InstanceSessionManager(AgentInfo agentInfo, string basePath) {
		this.agentInfo = agentInfo;
		this.basePath = basePath;
	}

	public CreateInstanceResult Create(CreateInstanceMessage message) {
		var info = message.Instance;
		var token = shutdownCancellationTokenSource.Token;

		try {
			semaphore.Wait(token);
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
				Path.Combine(basePath, "server.jar"), // TODO
				new ServerProperties(info.ServerPort, info.RconPort)
			);

			VanillaLauncher launcher = new VanillaLauncher(properties);

			instances.Add(info.InstanceGuid, new Instance(info, launcher));
			usedPorts.Add(info.ServerPort);
			usedPorts.Add(info.RconPort);

			return CreateInstanceResult.Success;
		} finally {
			semaphore.Release();
		}
	}

	public async Task<SetInstanceStateResult> Update(SetInstanceStateMessage message) {
		var token = shutdownCancellationTokenSource.Token;
		
		try {
			await semaphore.WaitAsync(token);
		} catch (OperationCanceledException) {
			return SetInstanceStateResult.AgentShuttingDown;
		}

		try {
			if (!instances.TryGetValue(message.InstanceGuid, out var instance)) {
				return SetInstanceStateResult.InstanceDoesNotExist;
			}

			if (message.IsRunning is {} isRunning) {
				if (isRunning) {
					await instance.Launch(token);
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

	// public async Task<LaunchResult> Start(Guid guid) {
	// 	if (!instanceLaunchers.TryGetValue(guid, out var launcher)) {
	// 		return new LaunchResult.InstanceNotFound();
	// 	}
	//
	// 	if (instanceSessions.ContainsKey(guid)) {
	// 		return new LaunchResult.InstanceAlreadyRunning();
	// 	}
	//
	// 	InstanceSession session;
	// 	try {
	// 		session = await launcher.Launch();
	// 	} catch (Exception e) {
	// 		return new LaunchResult.UnknownError(e);
	// 	}
	//
	// 	instanceSessions.Add(guid, session);
	// 	return new LaunchResult.Success(guid, session);
	// }
	//
	// public abstract record LaunchResult {
	// 	private LaunchResult() {}
	//
	// 	public sealed record Success(Guid InstanceGuid, InstanceSession Session) : LaunchResult;
	//
	// 	public sealed record UnknownError(Exception Exception) : LaunchResult;
	//
	// 	public sealed record InstanceNotFound : LaunchResult;
	//
	// 	public sealed record InstanceAlreadyRunning : LaunchResult;
	// }
	//
	// public async Task<SendCommandResult> SendCommand(Guid guid, string command) {
	// 	if (!instanceSessions.TryGetValue(guid, out var session)) {
	// 		return new SendCommandResult.InstanceNotRunning();
	// 	}
	//
	// 	await session.SendCommand(command);
	// 	return new SendCommandResult.Success();
	// }
	//
	// public abstract record SendCommandResult {
	// 	private SendCommandResult() {}
	//
	// 	public sealed record Success : SendCommandResult;
	//
	// 	public sealed record InstanceNotRunning : SendCommandResult;
	// }

	public async Task StopAll() {
		shutdownCancellationTokenSource.Cancel();
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
