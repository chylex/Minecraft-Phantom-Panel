using MemoryPack;

namespace Phantom.Common.Data.Instance;

[MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record InstancePlayerCounts(
	[property: MemoryPackOrder(0)] int Online,
	[property: MemoryPackOrder(1)] int Maximum
);
