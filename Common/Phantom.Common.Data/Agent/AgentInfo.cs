using MemoryPack;

namespace Phantom.Common.Data.Agent;

[MemoryPackable]
public sealed partial record AgentInfo(
	[property: MemoryPackOrder(0)] Guid Guid,
	[property: MemoryPackOrder(1)] string Name,
	[property: MemoryPackOrder(2)] ushort Version,
	[property: MemoryPackOrder(3)] ushort MaxInstances,
	[property: MemoryPackOrder(4)] RamAllocationUnits MaxMemory,
	[property: MemoryPackOrder(5)] AllowedPorts AllowedServerPorts,
	[property: MemoryPackOrder(6)] AllowedPorts AllowedRconPorts
);
