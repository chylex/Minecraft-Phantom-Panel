using Phantom.Common.Data;

namespace Phantom.Controller.Services.Agents;

public sealed record AgentStats(
	int RunningInstanceCount,
	RamAllocationUnits RunningInstanceMemory
);
