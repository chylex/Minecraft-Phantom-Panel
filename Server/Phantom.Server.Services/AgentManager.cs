using System.Collections.Concurrent;
using System.Text;

namespace Phantom.Server.Services; 

public sealed class AgentManager {
	public string AuthToken { get; }
	public ReadOnlySpan<byte> AuthTokenSpan => authTokenBytes;
	
	private readonly byte[] authTokenBytes;
	private readonly ConcurrentDictionary<Guid, AgentInfo> agents = new ();

	public AgentManager(string authToken) {
		this.AuthToken = authToken;
		this.authTokenBytes = Encoding.ASCII.GetBytes(authToken);
	}
	
	internal bool RegisterAgent(Guid guid, AgentInfo agentInfo) {
		return agents.TryAdd(guid, agentInfo);
	}
}
