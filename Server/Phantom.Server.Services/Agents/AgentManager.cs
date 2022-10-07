using System.Collections.Immutable;
using Phantom.Common.Data.Agent;
using Phantom.Common.Data.Replies;
using Phantom.Common.Logging;
using Phantom.Common.Messages;
using Phantom.Common.Messages.ToAgent;
using Phantom.Server.Database;
using Phantom.Server.Rpc;
using Phantom.Server.Services.Instances;
using Phantom.Server.Services.Rpc;
using Phantom.Utils.Collections;
using Phantom.Utils.Events;
using Serilog;

namespace Phantom.Server.Services.Agents;

public sealed class AgentManager {
	private static readonly ILogger Logger = PhantomLogger.Create<AgentManager>();

	private readonly ObservableAgents agents = new (PhantomLogger.Create<AgentManager, ObservableAgents>());

	public EventSubscribers<ImmutableArray<Agent>> AgentsChanged => agents.Subs;

	private readonly CancellationToken cancellationToken;
	private readonly AgentAuthToken authToken;
	private readonly DatabaseProvider databaseProvider;

	public AgentManager(ServiceConfiguration configuration, AgentAuthToken authToken, DatabaseProvider databaseProvider) {
		this.cancellationToken = configuration.CancellationToken;
		this.authToken = authToken;
		this.databaseProvider = databaseProvider;
	}

	public async Task Initialize() {
		using var scope = databaseProvider.CreateScope();

		await foreach (var entity in scope.Ctx.Agents.AsAsyncEnumerable().WithCancellation(cancellationToken)) {
			var agent = new Agent(entity.AgentGuid, entity.Name, entity.Version, entity.MaxInstances, entity.MaxMemory);
			if (!agents.TryRegister(agent)) {
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
			Connection = new AgentConnection(connection)
		};

		agents.Register(agent);

		using (var scope = databaseProvider.CreateScope()) {
			var entity = scope.Ctx.AgentUpsert.Fetch(agent.Guid);
			
			entity.Name = agent.Name;
			entity.Version = agent.Version;
			entity.MaxInstances = agent.MaxInstances;
			entity.MaxMemory = agent.MaxMemory;
			
			await scope.Ctx.SaveChangesAsync(cancellationToken);
		}

		Logger.Information("Registered agent \"{Name}\" (GUID {Guid}).", agent.Name, agent.Guid);

		await connection.Send(new RegisterAgentSuccessMessage(instanceManager.GetInstanceConfigurationsForAgent(agent.Guid)));
		return true;
	}

	internal bool UnregisterAgent(Guid agentGuid, RpcClientConnection connection) {
		if (agents.TryUnregister(agentGuid, connection)) {
			Logger.Information("Unregistered agent with GUID {Guid}.", agentGuid);
			return true;
		}
		else {
			return false;
		}
	}

	internal Agent? GetAgent(Guid guid) {
		return agents.GetAgent(guid);
	}

	internal void NotifyAgentIsAlive(Guid agentGuid) {
		// TODO automatically mark agent as offline if it doesn't send pings
		agents.Update(agentGuid, static agent => agent with { LastPing = DateTimeOffset.Now });
	}

	public async Task<bool> SendMessage<TMessage>(Guid guid, TMessage message) where TMessage : IMessageToAgent {
		var connection = agents.GetConnection(guid);
		if (connection != null) {
			await connection.SendMessage(message);
			return true;
		}
		else {
			// TODO handle missing agent?
			return false;
		}
	}

	internal async Task<int?> SendMessageWithReply<TMessage>(Guid guid, Func<uint, TMessage> messageFactory, TimeSpan waitForReplyTime) where TMessage : IMessageToAgent, IMessageWithReply {
		var sequenceId = MessageReplyTracker.Instance.RegisterReply();
		var message = messageFactory(sequenceId);

		if (!await SendMessage(guid, message)) {
			MessageReplyTracker.Instance.ForgetReply(sequenceId);
			return null;
		}

		return await MessageReplyTracker.Instance.WaitForReply(sequenceId, waitForReplyTime, cancellationToken);
	}

	private sealed class ObservableAgents : ObservableState<ImmutableArray<Agent>> {
		private readonly RwLockedDictionary<Guid, Agent> agents = new (LockRecursionPolicy.NoRecursion);

		public ObservableAgents(ILogger logger) : base(logger) {}

		public bool TryRegister(Agent agent) {
			return UpdateIf(agents.TryAddOrReplace(agent.Guid, agent, static oldAgent => oldAgent.IsOffline));
		}

		public void Register(Agent agent) {
			if (agents.AddOrReplace(agent.Guid, agent, out var oldAgent) && oldAgent.Connection is {} oldConnection) {
				oldConnection.Close();
			}

			Update();
		}

		public void Update(Guid guid, Func<Agent, Agent> updater) {
			UpdateIf(agents.TryReplace(guid, updater));
		}

		public bool TryUnregister(Guid guid, RpcClientConnection connection) {
			return UpdateIf(agents.TryReplaceIf(guid, static oldAgent => oldAgent.AsOffline(), oldAgent => oldAgent.Connection?.IsSame(connection) == true));
		}

		public Agent? GetAgent(Guid guid) {
			return agents.TryGetValue(guid, out var agent) ? agent : null;
		}

		public AgentConnection? GetConnection(Guid guid) {
			return agents.TryGetValue(guid, out var agent) ? agent.Connection : null;
		}

		protected override ImmutableArray<Agent> GetData() {
			return agents.ValuesCopy;
		}
	}
}
