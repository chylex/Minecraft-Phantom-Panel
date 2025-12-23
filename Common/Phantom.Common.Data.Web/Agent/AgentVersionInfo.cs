using MemoryPack;

namespace Phantom.Common.Data.Web.Agent;

[MemoryPackable(GenerateType.VersionTolerant)]
public readonly partial record struct AgentVersionInfo(
	[property: MemoryPackOrder(0)] ushort ProtocolVersion,
	[property: MemoryPackOrder(1)] string BuildVersion
);
