using MessagePack;

namespace Phantom.Common.Data; 

[MessagePackObject]
public sealed record InstanceInfo(
	[property: Key(0)] Guid AgentGuid,
	[property: Key(1)] Guid InstanceGuid,
	[property: Key(2)] string InstanceName,
	[property: Key(3)] ushort InstancePort,
	[property: Key(4)] string MinecraftVersion,
	[property: Key(5)] MinecraftServerKind MinecraftServerKind,
	[property: Key(6)] RamAllocationUnits MemoryAllocation
);
