using MemoryPack;

namespace Phantom.Common.Data.Agent;

[MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record AgentStats(
	[property: MemoryPackOrder(0)] int RunningInstanceCount,
	[property: MemoryPackOrder(1)] RamAllocationUnits RunningInstanceMemory
);
