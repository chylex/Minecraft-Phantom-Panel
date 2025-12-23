using System.Collections.Immutable;
using MemoryPack;
using Phantom.Common.Data.Agent;

namespace Phantom.Common.Data.Web.Agent;

[MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record Agent(
	[property: MemoryPackOrder(0)] Guid AgentGuid,
	[property: MemoryPackOrder(1)] AgentConfiguration Configuration,
	[property: MemoryPackOrder(2)] ImmutableArray<byte> ConnectionKey,
	[property: MemoryPackOrder(3)] AgentRuntimeInfo RuntimeInfo,
	[property: MemoryPackOrder(4)] AgentStats? Stats,
	[property: MemoryPackOrder(5)] IAgentConnectionStatus ConnectionStatus
) {
	[MemoryPackIgnore]
	public RamAllocationUnits? AvailableMemory => RuntimeInfo.MaxMemory - Stats?.RunningInstanceMemory;
}
