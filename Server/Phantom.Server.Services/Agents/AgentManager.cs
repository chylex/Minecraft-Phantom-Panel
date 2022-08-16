using System.Collections.Immutable;
using Phantom.Common.Data;
using Phantom.Common.Rpc.Message;
using Phantom.Common.Rpc.Messages.ToAgent;
using Phantom.Common.Rpc.Messages.ToServer;
using Phantom.Server.Rpc;
using Phantom.Utils.Collections;
using Phantom.Utils.Events;
using Phantom.Utils.Logging;
using Serilog;

namespace Phantom.Server.Services.Agents;

public sealed class AgentManager {
	private static readonly ILogger Logger = PhantomLogger.Create<AgentManager>();

	private readonly ObservableAgents agents = new ();

	public AgentAuthToken AuthToken { get; }
	public EventSubscribers<ImmutableArray<AgentInfo>> AgentInfoChanged => agents.Subs;

	internal AgentManager(AgentAuthToken authToken) {
		this.AuthToken = authToken;
	}

	internal RegisterAgentResultMessage RegisterAgent(RegisterAgentMessage message, RpcClientConnection connection) {
		if (!AuthToken.FixedTimeEquals(message.AuthToken)) {
			return RegisterAgentResultMessage.WithError("Invalid agent auth token.");
		}
		else if (!agents.TryRegister(new AgentConnection(connection, message.AgentInfo))) {
			return RegisterAgentResultMessage.WithError("Agent registration failed.");
		}
		else {
			Logger.Information("Registered agent \"{Name}\" (GUID {Guid}).", message.AgentInfo.Name, message.AgentInfo.Guid);
			return RegisterAgentResultMessage.WithSuccess;
		}
	}

	internal void UnregisterAgent(UnregisterAgentMessage message, RpcClientConnection connection) {
		if (agents.TryUnregister(message.AgentGuid, connection)) {
			Logger.Information("Unregistered agent with GUID {Guid}.", message.AgentGuid);
		}
	}

	public async Task SendMessage<TMessage>(Guid guid, TMessage message) where TMessage : IMessageToAgent {
		var connection = agents.GetConnection(guid);
		if (connection != null) {
			await connection.SendMessage(message);
		}
		// TODO handle missing agent?
	}

	private sealed class ObservableAgents : ObservableState<ImmutableArray<AgentInfo>> {
		private readonly RwLockedDictionary<Guid, AgentConnection> agents = new (LockRecursionPolicy.NoRecursion);

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
