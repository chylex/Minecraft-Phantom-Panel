using MessagePack;
using Phantom.Common.Data.Instance;

namespace Phantom.Common.Data.Agent;

[MessagePackObject]
public sealed record AgentInfo(
	[property: Key(0)] Guid Guid,
	[property: Key(1)] string Name,
	[property: Key(2)] ushort Version,
	[property: Key(3)] ushort MaxInstances,
	[property: Key(4)] RamAllocationUnits MaxMemory
);
