using Phantom.Common.Data.Agent;
using Phantom.Common.Data.Replies;
using Phantom.Common.Logging;
using Phantom.Common.Messages.ToAgent;
using Phantom.Server.Rpc;
using Serilog;

namespace Phantom.Server.Services.Agents;

public sealed class AgentManager {
	private static readonly ILogger Logger = PhantomLogger.Create<AgentManager>();

	private readonly CancellationToken cancellationToken;
	private readonly AgentAuthToken authToken;

	public AgentManager(ServiceConfiguration configuration, AgentAuthToken authToken) {
		this.cancellationToken = configuration.CancellationToken;
		this.authToken = authToken;
	}

	internal async Task<bool> RegisterAgent(AgentAuthToken authToken, AgentInfo agentInfo, RpcClientConnection connection) {
		if (!this.authToken.FixedTimeEquals(authToken)) {
			await connection.Send(new RegisterAgentFailureMessage(RegisterAgentFailure.InvalidToken));
			return false;
		}

		Logger.Information("Registered agent \"{Name}\" (GUID {Guid}).", agentInfo.Name, agentInfo.Guid);

		await connection.Send(new RegisterAgentSuccessMessage());
		return true;
	}

	internal void UnregisterAgent(Guid agentGuid, RpcClientConnection connection) {
		Logger.Information("Unregistered agent with GUID {Guid}.", agentGuid);
	}

	internal void NotifyAgentIsAlive(Guid agentGuid) {
		Logger.Verbose("Agent with GUID {Guid} is alive.", agentGuid);
	}
}
