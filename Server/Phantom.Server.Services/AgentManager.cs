using System.Collections.Concurrent;

namespace Phantom.Server.Services; 

public sealed class AgentManager {
	private readonly ConcurrentDictionary<Guid, AgentInfo> agents = new ();

	internal bool RegisterAgent(Guid guid, AgentInfo agentInfo) {
		return agents.TryAdd(guid, agentInfo);
	}
}
