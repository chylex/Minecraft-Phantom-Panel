using System.Collections.Immutable;
using Phantom.Common.Data;
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

	private sealed class ObservableAgents : ObservableState<ImmutableArray<AgentInfo>> {
		private readonly Dictionary<Guid, AgentConnection> agents = new ();
		private readonly ReaderWriterLockSlim agentsLock = new (LockRecursionPolicy.NoRecursion);

		public bool TryRegister(AgentConnection agentConnection) {
			agentsLock.EnterWriteLock();
			bool success = agents.TryAdd(agentConnection.Info.Guid, agentConnection);
			agentsLock.ExitWriteLock();

			if (success) {
				Update();
			}

			return success;
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
