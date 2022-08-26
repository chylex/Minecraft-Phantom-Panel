using Phantom.Common.Data;

namespace Phantom.Server.Services.Agents; 

public sealed class Agent {
	public Guid Guid { get; }
	public string Name { get; }
	public AgentInfo? CurrentInfo { get; private set; }
}
