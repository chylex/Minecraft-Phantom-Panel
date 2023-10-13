using MemoryPack;
using Phantom.Common.Data.Agent;

namespace Phantom.Common.Data.Web.Agent;

[MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record AgentWithStats(
	[property: MemoryPackOrder(0)] Guid Guid,
	[property: MemoryPackOrder(1)] string Name,
	[property: MemoryPackOrder(2)] ushort ProtocolVersion,
	[property: MemoryPackOrder(3)] string BuildVersion,
	[property: MemoryPackOrder(4)] ushort MaxInstances,
	[property: MemoryPackOrder(5)] RamAllocationUnits MaxMemory,
	[property: MemoryPackOrder(6)] AllowedPorts? AllowedServerPorts,
	[property: MemoryPackOrder(7)] AllowedPorts? AllowedRconPorts,
	[property: MemoryPackOrder(8)] AgentStats? Stats,
	[property: MemoryPackOrder(9)] DateTimeOffset? LastPing,
	[property: MemoryPackOrder(10)] bool IsOnline
) {
	[MemoryPackIgnore]
	public RamAllocationUnits? AvailableMemory => MaxMemory - Stats?.RunningInstanceMemory;
}
