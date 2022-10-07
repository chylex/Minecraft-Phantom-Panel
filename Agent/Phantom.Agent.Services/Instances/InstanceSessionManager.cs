using Phantom.Agent.Rpc;
using Phantom.Common.Data;
using Phantom.Common.Data.Agent;
using Phantom.Common.Data.Instance;
using Phantom.Common.Data.Replies;
using Phantom.Common.Logging;
using Phantom.Common.Messages.ToServer;
using Serilog;

namespace Phantom.Agent.Services.Instances;

sealed class InstanceSessionManager : IDisposable {
	private static readonly ILogger Logger = PhantomLogger.Create<InstanceSessionManager>();
	
	private readonly AgentInfo agentInfo;
	private readonly string basePath;

	private readonly Dictionary<Guid, InstanceConfiguration> instances = new ();

	private readonly CancellationTokenSource shutdownCancellationTokenSource = new ();
	private readonly CancellationToken shutdownCancellationToken;
	private readonly SemaphoreSlim semaphore = new (1, 1);

	public InstanceSessionManager(AgentInfo agentInfo, AgentFolders agentFolders) {
		this.agentInfo = agentInfo;
		this.basePath = agentFolders.InstancesFolderPath;
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
			var otherInstances = instances.Values.Where(inst => inst.InstanceGuid != instanceGuid).ToArray();
			if (otherInstances.Length + 1 > agentInfo.MaxInstances) {
				return ConfigureInstanceResult.InstanceLimitExceeded;
			}

			var availableMemory = agentInfo.MaxMemory - otherInstances.Aggregate(RamAllocationUnits.Zero, static (total, instance) => total + instance.MemoryAllocation);
			if (availableMemory < configuration.MemoryAllocation) {
				return ConfigureInstanceResult.MemoryLimitExceeded;
			}

			var instanceFolder = Path.Combine(basePath, instanceGuid.ToString());
			Directory.CreateDirectory(instanceFolder);

			instances[instanceGuid] = configuration;
			
			Logger.Information("Configured instance \"{Name}\" (GUID {Guid}).", configuration.InstanceName, configuration.InstanceGuid);
			
			await ServerMessaging.SendMessage(new ReportInstanceStatusMessage(instanceGuid, InstanceStatus.IsNotRunning));
			return ConfigureInstanceResult.Success;
		} finally {
			semaphore.Release();
		}
	}

	public void Dispose() {
		shutdownCancellationTokenSource.Dispose();
		semaphore.Dispose();
	}
}
