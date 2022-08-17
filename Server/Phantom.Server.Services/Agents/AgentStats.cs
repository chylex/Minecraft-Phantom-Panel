using Phantom.Common.Data;

namespace Phantom.Server.Services.Agents;

public sealed record AgentStats(
	AgentInfo AgentInfo,
	int UsedInstances,
	RamAllocationUnits UsedMemory
) {
	public RamAllocationUnits AvailableMemory => AgentInfo.MaxMemory - UsedMemory;
}
