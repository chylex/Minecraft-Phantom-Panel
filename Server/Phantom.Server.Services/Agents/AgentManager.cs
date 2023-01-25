using System.Collections.Immutable;
using Phantom.Common.Data.Agent;
using Phantom.Common.Data.Replies;
using Phantom.Common.Logging;
using Phantom.Common.Messages;
using Phantom.Common.Messages.ToAgent;
using Phantom.Server.Database;
using Phantom.Server.Rpc;
using Phantom.Server.Services.Instances;
using Phantom.Utils.Collections;
using Phantom.Utils.Events;
using Phantom.Utils.Runtime;
using ILogger = Serilog.ILogger;

namespace Phantom.Server.Services.Agents;

public sealed class AgentManager {
	private static readonly ILogger Logger = PhantomLogger.Create<AgentManager>();

	private static readonly TimeSpan DisconnectionRecheckInterval = TimeSpan.FromSeconds(5);
	private static readonly TimeSpan DisconnectionThreshold = TimeSpan.FromSeconds(12);

	private readonly ObservableAgents agents = new (PhantomLogger.Create<AgentManager, ObservableAgents>());

	public EventSubscribers<ImmutableArray<Agent>> AgentsChanged => agents.Subs;

	private readonly CancellationToken cancellationToken;
	private readonly AgentAuthToken authToken;
	private readonly DatabaseProvider databaseProvider;

	public AgentManager(ServiceConfiguration configuration, AgentAuthToken authToken, DatabaseProvider databaseProvider, TaskManager taskManager) {
		this.cancellationToken = configuration.CancellationToken;
		this.authToken = authToken;
		this.databaseProvider = databaseProvider;
		taskManager.Run("Refresh agent status loop", RefreshAgentStatus);
	}

	public async Task Initialize() {
		using var scope = databaseProvider.CreateScope();

		await foreach (var entity in scope.Ctx.Agents.AsAsyncEnumerable().WithCancellation(cancellationToken)) {
			var agent = new Agent(entity.AgentGuid, entity.Name, entity.ProtocolVersion, entity.BuildVersion, entity.MaxInstances, entity.MaxMemory);
			if (!agents.ByGuid.AddOrReplaceIf(agent.Guid, agent, static oldAgent => oldAgent.IsOffline)) {
				// TODO
				throw new InvalidOperationException("Unable to register agent from database: " + agent.Guid);
			}
		}
	}

	internal async Task<bool> RegisterAgent(AgentAuthToken authToken, AgentInfo agentInfo, InstanceManager instanceManager, RpcClientConnection connection) {
		if (!this.authToken.FixedTimeEquals(authToken)) {
			await connection.Send(new RegisterAgentFailureMessage(RegisterAgentFailure.InvalidToken));
			return false;
		}

		var agent = new Agent(agentInfo) {
			LastPing = DateTimeOffset.Now,
			IsOnline = true,
			Connection = new AgentConnection(connection)
		};

		if (agents.ByGuid.AddOrReplace(agent.Guid, agent, out var oldAgent)) {
			oldAgent.Connection?.Close();
		}

		using (var scope = databaseProvider.CreateScope()) {
			var entity = scope.Ctx.AgentUpsert.Fetch(agent.Guid);

			entity.Name = agent.Name;
			entity.ProtocolVersion = agent.ProtocolVersion;
			entity.BuildVersion = agent.BuildVersion;
			entity.MaxInstances = agent.MaxInstances;
			entity.MaxMemory = agent.MaxMemory;

			await scope.Ctx.SaveChangesAsync(cancellationToken);
		}

		Logger.Information("Registered agent \"{Name}\" (GUID {Guid}).", agent.Name, agent.Guid);

		await connection.Send(new RegisterAgentSuccessMessage(instanceManager.GetInstanceConfigurationsForAgent(agent.Guid)));
		return true;
	}

	internal bool UnregisterAgent(Guid agentGuid, RpcClientConnection connection) {
		if (agents.ByGuid.TryReplaceIf(agentGuid, static oldAgent => oldAgent.AsOffline(), oldAgent => oldAgent.Connection?.IsSame(connection) == true)) {
			Logger.Information("Unregistered agent with GUID {Guid}.", agentGuid);
			return true;
		}
		else {
			return false;
		}
	}

	internal Agent? GetAgent(Guid guid) {
		return agents.ByGuid.TryGetValue(guid, out var agent) ? agent : null;
	}

	internal void NotifyAgentIsAlive(Guid agentGuid) {
		agents.ByGuid.TryReplace(agentGuid, static agent => agent with { LastPing = DateTimeOffset.Now });
	}

	private async Task RefreshAgentStatus() {
		static Agent MarkAgentAsOffline(Agent agent) {
			Logger.Warning("Lost connection to agent \"{Name}\" (GUID {Guid}).", agent.Name, agent.Guid);
			return agent.AsDisconnected();
		}

		while (!cancellationToken.IsCancellationRequested) {
			await Task.Delay(DisconnectionRecheckInterval, cancellationToken);

			var now = DateTimeOffset.Now;
			agents.ByGuid.ReplaceAllIf(MarkAgentAsOffline, agent => agent.IsOnline && agent.LastPing is {} lastPing && now - lastPing >= DisconnectionThreshold);
		}
	}

	internal async Task<TReply?> SendMessage<TMessage, TReply>(Guid guid, TMessage message, TimeSpan waitForReplyTime) where TMessage : IMessageToAgent<TReply> where TReply : class {
		var connection = agents.ByGuid.TryGetValue(guid, out var agent) ? agent.Connection : null;
		if (connection == null) {
			// TODO handle missing agent?
			return null;
		}

		return await connection.Send<TMessage, TReply>(message, waitForReplyTime, cancellationToken);
	}

	private sealed class ObservableAgents : ObservableState<ImmutableArray<Agent>> {
		public RwLockedObservableDictionary<Guid, Agent> ByGuid { get; } = new (LockRecursionPolicy.NoRecursion);

		public ObservableAgents(ILogger logger) : base(logger) {
			ByGuid.CollectionChanged += Update;
		}

		protected override ImmutableArray<Agent> GetData() {
			return ByGuid.ValuesCopy;
		}
	}
}
