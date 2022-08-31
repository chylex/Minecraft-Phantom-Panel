using Phantom.Common.Data.Agent;
using Phantom.Common.Data.Instance;

namespace Phantom.Server.Services.Agents;

public sealed record AgentStats(
	AgentInfo AgentInfo,
	int UsedInstances,
	RamAllocationUnits UsedMemory
) {
	public RamAllocationUnits AvailableMemory => AgentInfo.MaxMemory - UsedMemory;
}
