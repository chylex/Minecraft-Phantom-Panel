using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Phantom.Common.Data;
using Phantom.Common.Data.Instance;
using Phantom.Common.Data.Minecraft;
using Phantom.Common.Data.Replies;
using Phantom.Common.Logging;
using Phantom.Common.Messages;
using Phantom.Common.Messages.ToAgent;
using Phantom.Controller.Database;
using Phantom.Controller.Database.Entities;
using Phantom.Controller.Minecraft;
using Phantom.Controller.Services.Agents;
using Phantom.Utils.Collections;
using Phantom.Utils.Events;
using ILogger = Serilog.ILogger;

namespace Phantom.Controller.Services.Instances;

public sealed class InstanceManager {
	private static readonly ILogger Logger = PhantomLogger.Create<InstanceManager>();

	private readonly ObservableInstances instances = new (PhantomLogger.Create<InstanceManager, ObservableInstances>());

	public EventSubscribers<ImmutableDictionary<Guid, Instance>> InstancesChanged => instances.Subs;

	private readonly CancellationToken cancellationToken;
	private readonly AgentManager agentManager;
	private readonly MinecraftVersions minecraftVersions;
	private readonly DatabaseProvider databaseProvider;
	private readonly SemaphoreSlim modifyInstancesSemaphore = new (1, 1);

	public InstanceManager(ServiceConfiguration configuration, AgentManager agentManager, MinecraftVersions minecraftVersions, DatabaseProvider databaseProvider) {
		this.cancellationToken = configuration.CancellationToken;
		this.agentManager = agentManager;
		this.minecraftVersions = minecraftVersions;
		this.databaseProvider = databaseProvider;
	}

	public async Task Initialize() {
		using var scope = databaseProvider.CreateScope();

		await foreach (var entity in scope.Ctx.Instances.AsAsyncEnumerable().WithCancellation(cancellationToken)) {
			var configuration = new InstanceConfiguration(
				entity.AgentGuid,
				entity.InstanceGuid,
				entity.InstanceName,
				entity.ServerPort,
				entity.RconPort,
				entity.MinecraftVersion,
				entity.MinecraftServerKind,
				entity.MemoryAllocation,
				entity.JavaRuntimeGuid,
				JvmArgumentsHelper.Split(entity.JvmArguments)
			);

			var instance = new Instance(configuration, entity.LaunchAutomatically);
			instances.ByGuid[instance.Configuration.InstanceGuid] = instance;
		}
	}

	[SuppressMessage("ReSharper", "ConvertIfStatementToConditionalTernaryExpression")]
	public async Task<InstanceActionResult<AddOrEditInstanceResult>> AddOrEditInstance(InstanceConfiguration configuration) {
		var agent = agentManager.GetAgent(configuration.AgentGuid);
		if (agent == null) {
			return InstanceActionResult.Concrete(AddOrEditInstanceResult.AgentNotFound);
		}

		if (string.IsNullOrWhiteSpace(configuration.InstanceName)) {
			return InstanceActionResult.Concrete(AddOrEditInstanceResult.InstanceNameMustNotBeEmpty);
		}
		
		if (configuration.MemoryAllocation <= RamAllocationUnits.Zero) {
			return InstanceActionResult.Concrete(AddOrEditInstanceResult.InstanceMemoryMustNotBeZero);
		}
		
		var serverExecutableInfo = await minecraftVersions.GetServerExecutableInfo(configuration.MinecraftVersion, cancellationToken);
		if (serverExecutableInfo == null) {
			return InstanceActionResult.Concrete(AddOrEditInstanceResult.MinecraftVersionDownloadInfoNotFound);
		}

		InstanceActionResult<AddOrEditInstanceResult> result;
		bool isNewInstance;

		await modifyInstancesSemaphore.WaitAsync(cancellationToken);
		try {
			isNewInstance = !instances.ByGuid.TryReplace(configuration.InstanceGuid, instance => instance with { Configuration = configuration });
			if (isNewInstance) {
				instances.ByGuid.TryAdd(configuration.InstanceGuid, new Instance(configuration));
			}

			var message = new ConfigureInstanceMessage(configuration, new InstanceLaunchProperties(serverExecutableInfo));
			var reply = await agentManager.SendMessage<ConfigureInstanceMessage, InstanceActionResult<ConfigureInstanceResult>>(configuration.AgentGuid, message, TimeSpan.FromSeconds(10));
			
			result = reply.DidNotReplyIfNull().Map(static result => result switch {
				ConfigureInstanceResult.Success => AddOrEditInstanceResult.Success,
				_                               => AddOrEditInstanceResult.UnknownError
			});
			
			if (result.Is(AddOrEditInstanceResult.Success)) {
				using var scope = databaseProvider.CreateScope();
				InstanceEntity entity = scope.Ctx.InstanceUpsert.Fetch(configuration.InstanceGuid);

				entity.AgentGuid = configuration.AgentGuid;
				entity.InstanceName = configuration.InstanceName;
				entity.ServerPort = configuration.ServerPort;
				entity.RconPort = configuration.RconPort;
				entity.MinecraftVersion = configuration.MinecraftVersion;
				entity.MinecraftServerKind = configuration.MinecraftServerKind;
				entity.MemoryAllocation = configuration.MemoryAllocation;
				entity.JavaRuntimeGuid = configuration.JavaRuntimeGuid;
				entity.JvmArguments = JvmArgumentsHelper.Join(configuration.JvmArguments);

				await scope.Ctx.SaveChangesAsync(cancellationToken);
			}
			else if (isNewInstance) {
				instances.ByGuid.Remove(configuration.InstanceGuid);
			}
		} finally {
			modifyInstancesSemaphore.Release();
		}
		
		if (result.Is(AddOrEditInstanceResult.Success)) {
			if (isNewInstance) {
				Logger.Information("Added instance \"{InstanceName}\" (GUID {InstanceGuid}) to agent \"{AgentName}\".", configuration.InstanceName, configuration.InstanceGuid, agent.Name);
			}
			else {
				Logger.Information("Edited instance \"{InstanceName}\" (GUID {InstanceGuid}) in agent \"{AgentName}\".", configuration.InstanceName, configuration.InstanceGuid, agent.Name);
			}
		}
		else {
			if (isNewInstance) {
				Logger.Information("Failed adding instance \"{InstanceName}\" (GUID {InstanceGuid}) to agent \"{AgentName}\". {ErrorMessage}", configuration.InstanceName, configuration.InstanceGuid, agent.Name, result.ToSentence(AddOrEditInstanceResultExtensions.ToSentence));
			}
			else {
				Logger.Information("Failed editing instance \"{InstanceName}\" (GUID {InstanceGuid}) in agent \"{AgentName}\". {ErrorMessage}", configuration.InstanceName, configuration.InstanceGuid, agent.Name, result.ToSentence(AddOrEditInstanceResultExtensions.ToSentence));
			}
		}

		return result;
	}

	public ImmutableDictionary<Guid, string> GetInstanceNames() {
		return instances.ByGuid.ToImmutable<string>(static instance => instance.Configuration.InstanceName);
	}

	public InstanceConfiguration? GetInstanceConfiguration(Guid instanceGuid) {
		return instances.ByGuid.TryGetValue(instanceGuid, out var instance) ? instance.Configuration : null;
	}

	internal void SetInstanceState(Guid instanceGuid, IInstanceStatus instanceStatus) {
		instances.ByGuid.TryReplace(instanceGuid, instance => instance with { Status = instanceStatus });
	}

	internal void SetInstanceStatesForAgent(Guid agentGuid, IInstanceStatus instanceStatus) {
		instances.ByGuid.ReplaceAllIf(instance => instance with { Status = instanceStatus }, instance => instance.Configuration.AgentGuid == agentGuid);
	}

	private async Task<InstanceActionResult<TReply>> SendInstanceActionMessage<TMessage, TReply>(Instance instance, TMessage message) where TMessage : IMessageToAgent<InstanceActionResult<TReply>> {
		var reply = await agentManager.SendMessage<TMessage, InstanceActionResult<TReply>>(instance.Configuration.AgentGuid, message, TimeSpan.FromSeconds(10));
		return reply.DidNotReplyIfNull();
	}

	private async Task<InstanceActionResult<TReply>> SendInstanceActionMessage<TMessage, TReply>(Guid instanceGuid, TMessage message) where TMessage : IMessageToAgent<InstanceActionResult<TReply>> {
		return instances.ByGuid.TryGetValue(instanceGuid, out var instance) ? await SendInstanceActionMessage<TMessage, TReply>(instance, message) : InstanceActionResult.General<TReply>(InstanceActionGeneralResult.InstanceDoesNotExist);
	}

	public async Task<InstanceActionResult<LaunchInstanceResult>> LaunchInstance(Guid instanceGuid) {
		var result = await SendInstanceActionMessage<LaunchInstanceMessage, LaunchInstanceResult>(instanceGuid, new LaunchInstanceMessage(instanceGuid));
		if (result.Is(LaunchInstanceResult.LaunchInitiated)) {
			await SetInstanceShouldLaunchAutomatically(instanceGuid, true);
		}

		return result;
	}

	public async Task<InstanceActionResult<StopInstanceResult>> StopInstance(Guid instanceGuid, MinecraftStopStrategy stopStrategy) {
		var result = await SendInstanceActionMessage<StopInstanceMessage, StopInstanceResult>(instanceGuid, new StopInstanceMessage(instanceGuid, stopStrategy));
		if (result.Is(StopInstanceResult.StopInitiated)) {
			await SetInstanceShouldLaunchAutomatically(instanceGuid, false);
		}

		return result;
	}

	private async Task SetInstanceShouldLaunchAutomatically(Guid instanceGuid, bool shouldLaunchAutomatically) {
		await modifyInstancesSemaphore.WaitAsync(cancellationToken);
		try {
			instances.ByGuid.TryReplace(instanceGuid, instance => instance with { LaunchAutomatically = shouldLaunchAutomatically });

			using var scope = databaseProvider.CreateScope();
			var entity = await scope.Ctx.Instances.FindAsync(instanceGuid, cancellationToken);
			if (entity != null) {
				entity.LaunchAutomatically = shouldLaunchAutomatically;
				await scope.Ctx.SaveChangesAsync(cancellationToken);
			}
		} finally {
			modifyInstancesSemaphore.Release();
		}
	}

	public async Task<InstanceActionResult<SendCommandToInstanceResult>> SendCommand(Guid instanceGuid, string command) {
		return await SendInstanceActionMessage<SendCommandToInstanceMessage, SendCommandToInstanceResult>(instanceGuid, new SendCommandToInstanceMessage(instanceGuid, command));
	}

	internal async Task<ImmutableArray<ConfigureInstanceMessage>> GetInstanceConfigurationsForAgent(Guid agentGuid) {
		var configurationMessages = ImmutableArray.CreateBuilder<ConfigureInstanceMessage>();
		
		foreach (var (configuration, _, launchAutomatically) in instances.ByGuid.ValuesCopy.Where(instance => instance.Configuration.AgentGuid == agentGuid)) {
			var serverExecutableInfo = await minecraftVersions.GetServerExecutableInfo(configuration.MinecraftVersion, cancellationToken);
			configurationMessages.Add(new ConfigureInstanceMessage(configuration, new InstanceLaunchProperties(serverExecutableInfo), launchAutomatically));
		}

		return configurationMessages.ToImmutable();
	}

	private sealed class ObservableInstances : ObservableState<ImmutableDictionary<Guid, Instance>> {
		public RwLockedObservableDictionary<Guid, Instance> ByGuid { get; } = new (LockRecursionPolicy.NoRecursion);

		public ObservableInstances(ILogger logger) : base(logger) {
			ByGuid.CollectionChanged += Update;
		}

		protected override ImmutableDictionary<Guid, Instance> GetData() {
			return ByGuid.ToImmutable();
		}
	}
}
