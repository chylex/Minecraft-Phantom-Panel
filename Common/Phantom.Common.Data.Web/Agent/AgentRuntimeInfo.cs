using MemoryPack;
using Phantom.Common.Data.Agent;

namespace Phantom.Common.Data.Web.Agent;

[MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record AgentRuntimeInfo(
	[property: MemoryPackOrder(0)] AgentVersionInfo? VersionInfo = null,
	[property: MemoryPackOrder(1)] ushort? MaxInstances = null,
	[property: MemoryPackOrder(2)] RamAllocationUnits? MaxMemory = null,
	[property: MemoryPackOrder(3)] AllowedPorts? AllowedServerPorts = null,
	[property: MemoryPackOrder(4)] AllowedPorts? AllowedRconPorts = null
) {
	public static AgentRuntimeInfo From(AgentInfo agentInfo) {
		return new AgentRuntimeInfo(new AgentVersionInfo(agentInfo.ProtocolVersion, agentInfo.BuildVersion), agentInfo.MaxInstances, agentInfo.MaxMemory, agentInfo.AllowedServerPorts, agentInfo.AllowedRconPorts);
	}
}
