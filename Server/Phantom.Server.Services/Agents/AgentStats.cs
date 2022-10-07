using Phantom.Common.Data;

namespace Phantom.Server.Services.Agents;

public sealed record AgentStats(
	Agent Agent,
	int UsedInstances,
	RamAllocationUnits UsedMemory
) {
	public RamAllocationUnits AvailableMemory => Agent.MaxMemory - UsedMemory;
}
