using MemoryPack;

namespace Phantom.Common.Data.Instance;

[MemoryPackable(GenerateType.VersionTolerant)]
public readonly partial record struct InstancePlayerCounts(
	[property: MemoryPackOrder(0)] int Online,
	[property: MemoryPackOrder(1)] int Maximum
);
