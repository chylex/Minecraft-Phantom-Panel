using System.Collections.Immutable;
using Phantom.Common.Data;
using Phantom.Common.Data.Replies;
using Phantom.Common.Logging;
using Phantom.Common.Messages;
using Phantom.Common.Messages.ToServer;
using Phantom.Server.Database;
using Phantom.Server.Rpc;
using Phantom.Server.Services.Rpc;
using Phantom.Utils.Collections;
using Phantom.Utils.Events;
using Serilog;

namespace Phantom.Server.Services.Agents;

public sealed class AgentManager {
	private static readonly ILogger Logger = PhantomLogger.Create<AgentManager>();

	private readonly ObservableAgentInfos agents = new (PhantomLogger.Create<AgentManager, ObservableAgentInfos>());

	public EventSubscribers<ImmutableArray<Agent>> AgentsChanged => agents.Subs;

	private readonly CancellationToken cancellationToken;
	private readonly AgentAuthToken authToken;
	private readonly DatabaseProvider databaseProvider;

	public AgentManager(ServiceConfiguration serviceConfiguration, AgentAuthToken authToken, DatabaseProvider databaseProvider) {
		this.cancellationToken = serviceConfiguration.CancellationToken;
		this.authToken = authToken;
		this.databaseProvider = databaseProvider;
	}

	public async Task Initialize() {
		using var scope = databaseProvider.CreateScope();
		
		await foreach (var agent in scope.Ctx.Agents.AsAsyncEnumerable().WithCancellation(cancellationToken)) {
			if (!agents.TryRegister(new Agent.Offline(agent.Id, agent.Name))) {
				// TODO
				throw new InvalidOperationException("Unable to register agent from database: " + agent.Id);
			}
		}
	}

	internal async Task<RegisterAgentResult> RegisterAgent(RegisterAgentMessage message, RpcClientConnection connection) {
		if (!authToken.FixedTimeEquals(message.AuthToken)) {
			return RegisterAgentResult.InvalidToken;
		}
		else if (!agents.TryRegister(new Agent.Online(new AgentConnection(connection, message.AgentInfo)))) {
			return RegisterAgentResult.OldConnectionNotClosed;
		}
		else {
			var agentGuid = message.AgentInfo.Guid;
			var agentName = message.AgentInfo.Name;
			
			using (var scope = databaseProvider.CreateScope()) {
				scope.Ctx.Agents.Upsert(agentGuid, (guid, agent) => {
					agent.Id = guid;
					agent.Name = agentName;
				});
				
				await scope.Ctx.SaveChangesAsync(cancellationToken);
			}
			
			Logger.Information("Registered agent \"{Name}\" (GUID {Guid}).", agentName, agentGuid);
			return RegisterAgentResult.Success;
		}
	}

	internal void UnregisterAgent(UnregisterAgentMessage message, RpcClientConnection connection) {
		if (agents.TryUnregister(message.AgentGuid, connection)) {
			Logger.Information("Unregistered agent with GUID {Guid}.", message.AgentGuid);
		}
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

	private sealed class ObservableAgentInfos : ObservableState<ImmutableArray<Agent>> {
		private readonly RwLockedDictionary<Guid, Agent> agents = new (LockRecursionPolicy.NoRecursion);
		
		public ObservableAgentInfos(ILogger logger) : base(logger) {}

		public bool TryRegister(Agent.Offline agent) {
			if (agents.TryAddOrReplace(agent.Guid, agent, static oldAgent => oldAgent is Agent.Offline)) {
				Update();
				return true;
			}
			else {
				return false;
			}
		}
		
		public bool TryRegister(Agent.Online agent) {
			if (agents.TryAddOrReplace(agent.Guid, agent, static oldAgent => oldAgent is not Agent.Online online || online.Connection.IsClosed)) {
				Update();
				return true;
			}
			else {
				return false;
			}
		}

		public bool TryUnregister(Guid guid, RpcClientConnection connection) {
			if (agents.TryReplace(guid, static oldAgent => oldAgent.AsOffline(), oldAgent => oldAgent is Agent.Online online && online.Connection.IsSame(connection))) {
				Update();
				return true;
			}
			else {
				return false;
			}
		}

		public AgentConnection? GetConnection(Guid guid) {
			return agents.TryGetValue(guid, out var agent) && agent is Agent.Online online ? online.Connection : null;
		}

		protected override ImmutableArray<Agent> GetData() {
			return agents.ValuesCopy;
		}
	}
}
