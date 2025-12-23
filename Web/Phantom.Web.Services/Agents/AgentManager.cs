using System.Collections.Immutable;
using Phantom.Common.Data;
using Phantom.Common.Data.Web.Agent;
using Phantom.Common.Data.Web.Users;
using Phantom.Common.Messages.Web.ToController;
using Phantom.Utils.Events;
using Phantom.Utils.Logging;
using Phantom.Web.Services.Authentication;
using Phantom.Web.Services.Rpc;

namespace Phantom.Web.Services.Agents;

using AgentDictionary = ImmutableDictionary<Guid, Agent>;

public sealed class AgentManager(ControllerConnection controllerConnection) {
	private readonly SimpleObservableState<AgentDictionary> agents = new (PhantomLogger.Create<AgentManager>("Agents"), AgentDictionary.Empty);
	
	public EventSubscribers<AgentDictionary> AgentsChanged => agents.Subs;
	
	internal void RefreshAgents(ImmutableArray<Agent> newAgents) {
		agents.SetTo(newAgents.ToImmutableDictionary(static agent => agent.AgentGuid));
	}
	
	public AgentDictionary GetAll() {
		return agents.Value;
	}
	
	public Agent? GetByGuid(AuthenticatedUser? authenticatedUser, Guid agentGuid) {
		if (authenticatedUser == null) {
			return null;
		}
		
		var agent = agents.Value.GetValueOrDefault(agentGuid);
		return agent != null && authenticatedUser.Info.HasAccessToAgent(agent.AgentGuid) ? agent : null;
	}
	
	public AgentDictionary ToDictionaryByGuid(AuthenticatedUser? authenticatedUser) {
		if (authenticatedUser == null) {
			return AgentDictionary.Empty;
		}
		
		return agents.Value
		             .Where(kvp => authenticatedUser.Info.HasAccessToAgent(kvp.Key))
		             .ToImmutableDictionary();
	}
	
	public async Task<Result<CreateOrUpdateAgentResult, UserActionFailure>> CreateOrUpdateAgent(AuthenticatedUser? authenticatedUser, Guid agentGuid, AgentConfiguration configuration, CancellationToken cancellationToken) {
		if (authenticatedUser != null && authenticatedUser.Info.CheckPermission(Permission.ManageAllAgents)) {
			var message = new CreateOrUpdateAgentMessage(authenticatedUser.Token, agentGuid, configuration);
			return await controllerConnection.Send<CreateOrUpdateAgentMessage, Result<CreateOrUpdateAgentResult, UserActionFailure>>(message, cancellationToken);
		}
		else {
			return UserActionFailure.NotAuthorized;
		}
	}
}
