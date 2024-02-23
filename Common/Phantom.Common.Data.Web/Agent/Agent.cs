using MemoryPack;
using Phantom.Common.Data.Agent;

namespace Phantom.Common.Data.Web.Agent;

[MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record Agent(
	[property: MemoryPackOrder(0)] Guid AgentGuid,
	[property: MemoryPackOrder(1)] AgentConfiguration Configuration,
	[property: MemoryPackOrder(2)] AgentStats? Stats,
	[property: MemoryPackOrder(3)] IAgentConnectionStatus ConnectionStatus
) {
	[MemoryPackIgnore]
	public RamAllocationUnits? AvailableMemory => Configuration.MaxMemory - Stats?.RunningInstanceMemory;
}
