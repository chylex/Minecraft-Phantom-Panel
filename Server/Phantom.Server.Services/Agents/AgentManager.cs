using System.Collections.Immutable;
using Phantom.Common.Data;
using Phantom.Common.Rpc.Message;
using Phantom.Common.Rpc.Messages.ToAgent;
using Phantom.Common.Rpc.Messages.ToServer;
using Phantom.Server.Rpc;
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
		agents.TryUnregister(message.AgentGuid, connection);
	}

	public async Task SendMessage<TMessage>(Guid guid, TMessage message) where TMessage : IMessageToAgent {
		var connection = agents.GetConnection(guid);
		if (connection != null) {
			await connection.SendMessage(message);
		}
		// TODO handle missing agent?
	}

	private sealed class ObservableAgents : ObservableState<ImmutableArray<AgentInfo>> {
		private readonly Dictionary<Guid, AgentConnection> agents = new ();
		private readonly ReaderWriterLockSlim agentsLock = new (LockRecursionPolicy.NoRecursion);

		public bool TryRegister(AgentConnection agentConnection) {
			agentsLock.EnterWriteLock();
			
			var guid = agentConnection.Info.Guid;
			bool success = !agents.TryGetValue(guid, out var oldConnection) || oldConnection.IsClosed;
			if (success) {
				agents[guid] = agentConnection;
			}
			
			agentsLock.ExitWriteLock();

			if (success) {
				Update();
			}

			return success;
		}

		public bool TryUnregister(Guid guid, RpcClientConnection connection) {
			agentsLock.EnterWriteLock();
			bool success = agents.TryGetValue(guid, out var agentConnection) && agentConnection.IsSame(connection) && agents.Remove(guid);
			agentsLock.ExitWriteLock();

			if (success) {
				Update();
			}
			
			return success;
		}

		public AgentConnection? GetConnection(Guid guid) {
			agentsLock.EnterReadLock();
			try {
				return agents.TryGetValue(guid, out var connection) ? connection : null;
			} finally {
				agentsLock.ExitReadLock();
			}
		}

		protected override ImmutableArray<AgentInfo> GetData() {
			agentsLock.EnterReadLock();
			try {
				return agents.Values.Select(static agent => agent.Info).ToImmutableArray();
			} finally {
				agentsLock.ExitReadLock();
			}
		}
	}
}
