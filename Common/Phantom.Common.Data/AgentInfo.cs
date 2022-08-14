using MessagePack;

namespace Phantom.Common.Data;

[MessagePackObject]
public sealed record AgentInfo(
	[property: Key(0)] Guid Guid,
	[property: Key(1)] ushort Version,
	[property: Key(2)] string Name,
	[property: Key(3)] ushort MaxInstances,
	[property: Key(4)] RamAllocationUnits MaxMemory
);
