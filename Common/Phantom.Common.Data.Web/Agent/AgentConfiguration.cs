using MemoryPack;
using Phantom.Common.Data.Agent;

namespace Phantom.Common.Data.Web.Agent;

[MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record AgentConfiguration(
	[property: MemoryPackOrder(0)] string AgentName,
	[property: MemoryPackOrder(1)] ushort ProtocolVersion,
	[property: MemoryPackOrder(2)] string BuildVersion,
	[property: MemoryPackOrder(3)] ushort MaxInstances,
	[property: MemoryPackOrder(4)] RamAllocationUnits MaxMemory,
	[property: MemoryPackOrder(5)] AllowedPorts? AllowedServerPorts = null,
	[property: MemoryPackOrder(6)] AllowedPorts? AllowedRconPorts = null
) {
	public static AgentConfiguration From(AgentInfo agentInfo) {
		return new AgentConfiguration(agentInfo.AgentName, agentInfo.ProtocolVersion, agentInfo.BuildVersion, agentInfo.MaxInstances, agentInfo.MaxMemory, agentInfo.AllowedServerPorts, agentInfo.AllowedRconPorts);
	}
}
