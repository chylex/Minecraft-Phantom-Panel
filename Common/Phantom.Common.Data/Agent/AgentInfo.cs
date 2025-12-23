using MemoryPack;

namespace Phantom.Common.Data.Agent;

[MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record AgentInfo(
	[property: MemoryPackOrder(0)] ushort ProtocolVersion,
	[property: MemoryPackOrder(1)] string BuildVersion,
	[property: MemoryPackOrder(2)] ushort MaxInstances,
	[property: MemoryPackOrder(3)] RamAllocationUnits MaxMemory,
	[property: MemoryPackOrder(4)] AllowedPorts AllowedServerPorts,
	[property: MemoryPackOrder(5)] AllowedPorts AllowedRconPorts
);
