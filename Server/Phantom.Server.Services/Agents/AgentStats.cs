using Phantom.Common.Data;

namespace Phantom.Server.Services.Agents;

public sealed record AgentStats(
	int RunningInstanceCount,
	RamAllocationUnits RunningInstanceMemory
);
