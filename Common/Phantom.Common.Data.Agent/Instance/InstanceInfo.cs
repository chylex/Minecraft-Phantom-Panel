using System.Collections.Immutable;
using MemoryPack;

namespace Phantom.Common.Data.Agent.Instance;

[MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record InstanceInfo(
	[property: MemoryPackOrder(0)] string InstanceName,
	[property: MemoryPackOrder(1)] ushort ServerPort,
	[property: MemoryPackOrder(2)] ImmutableSortedSet<ushort> AdditionalPorts,
	[property: MemoryPackOrder(3)] RamAllocationUnits MemoryAllocation
);
