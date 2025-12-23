using MemoryPack;

namespace Phantom.Common.Data.Web.Agent;

[MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record AgentConfiguration(
	[property: MemoryPackOrder(0)] string AgentName
);
