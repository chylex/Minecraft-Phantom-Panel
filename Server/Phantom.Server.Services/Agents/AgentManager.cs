using System.Collections.Immutable;
using Phantom.Common.Data;
using Phantom.Common.Data.Replies;
using Phantom.Common.Logging;
using Phantom.Common.Messages;
using Phantom.Common.Messages.ToServer;
using Phantom.Server.Database;
using Phantom.Server.Database.Entities;
using Phantom.Server.Rpc;
using Phantom.Server.Services.Rpc;
using Phantom.Utils.Collections;
using Phantom.Utils.Events;
using Serilog;

namespace Phantom.Server.Services.Agents;

public sealed class AgentManager {
	private static readonly ILogger Logger = PhantomLogger.Create<AgentManager>();

	private readonly ObservableAgentInfos agentInfos = new (PhantomLogger.Create<AgentManager, ObservableAgentInfos>());

	public EventSubscribers<ImmutableArray<AgentInfo>> AgentsInfosChanged => agentInfos.Subs;

	private readonly CancellationToken cancellationToken;
	private readonly AgentAuthToken authToken;
	private readonly DatabaseProvider databaseProvider;

	public AgentManager(ServiceConfiguration serviceConfiguration, AgentAuthToken authToken, DatabaseProvider databaseProvider) {
		this.cancellationToken = serviceConfiguration.CancellationToken;
		this.authToken = authToken;
		this.databaseProvider = databaseProvider;
	}

	internal async Task<RegisterAgentResult> RegisterAgent(RegisterAgentMessage message, RpcClientConnection connection) {
		if (!authToken.FixedTimeEquals(message.AuthToken)) {
			return RegisterAgentResult.InvalidToken;
		}
		else if (!agentInfos.TryRegister(new AgentConnection(connection, message.AgentInfo))) {
			return RegisterAgentResult.OldConnectionNotClosed;
		}
		else {
			using (var scope = databaseProvider.CreateScope()) {
				scope.Db.Agents.Add(new AgentEntity(message.AgentInfo.Guid, message.AgentInfo.Name));
				await scope.Db.SaveChangesAsync(cancellationToken);
			}
			
			Logger.Information("Registered agent \"{Name}\" (GUID {Guid}).", message.AgentInfo.Name, message.AgentInfo.Guid);
			return RegisterAgentResult.Success;
		}
	}

	internal void UnregisterAgent(UnregisterAgentMessage message, RpcClientConnection connection) {
		if (agentInfos.TryUnregister(message.AgentGuid, connection)) {
			Logger.Information("Unregistered agent with GUID {Guid}.", message.AgentGuid);
		}
	}

	public async Task<bool> SendMessage<TMessage>(Guid guid, TMessage message) where TMessage : IMessageToAgent {
		var connection = agentInfos.GetConnection(guid);
		if (connection != null) {
			await connection.SendMessage(message);
			return true;
		}
		else {
			// TODO handle missing agent?
			return false;
		}
	}

	public async Task<int?> SendMessageWithReply<TMessage>(Guid guid, Func<uint, TMessage> messageFactory, TimeSpan waitForReplyTime) where TMessage : IMessageToAgent, IMessageWithReply {
		var sequenceId = MessageReplyTracker.Instance.RegisterReply();
		var message = messageFactory(sequenceId);

		if (!await SendMessage(guid, message)) {
			MessageReplyTracker.Instance.ForgetReply(sequenceId);
			return null;
		}

		return await MessageReplyTracker.Instance.WaitForReply(sequenceId, waitForReplyTime, cancellationToken);
	}

	private sealed class ObservableAgentInfos : ObservableState<ImmutableArray<AgentInfo>> {
		private readonly RwLockedDictionary<Guid, AgentConnection> agents = new (LockRecursionPolicy.NoRecursion);
		
		public ObservableAgentInfos(ILogger logger) : base(logger) {}

		public bool TryRegister(AgentConnection agentConnection) {
			if (agents.TryAddOrReplace(agentConnection.Info.Guid, agentConnection, static oldConnection => oldConnection.IsClosed)) {
				Update();
				return true;
			}
			else {
				return false;
			}
		}

		public bool TryUnregister(Guid guid, RpcClientConnection connection) {
			if (agents.TryRemove(guid, oldConnection => oldConnection.IsSame(connection))) {
				Update();
				return true;
			}
			else {
				return false;
			}
		}

		public AgentConnection? GetConnection(Guid guid) {
			return agents.TryGetValue(guid, out var connection) ? connection : null;
		}

		protected override ImmutableArray<AgentInfo> GetData() {
			return agents.ValuesCopy.Select(static agent => agent.Info).ToImmutableArray();
		}
	}
}
