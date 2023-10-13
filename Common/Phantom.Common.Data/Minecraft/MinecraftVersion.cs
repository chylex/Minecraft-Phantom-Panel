using MemoryPack;

namespace Phantom.Common.Data.Minecraft;

[MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record MinecraftVersion(
	[property: MemoryPackOrder(0)] string Id,
	[property: MemoryPackOrder(1)] MinecraftVersionType Type,
	[property: MemoryPackOrder(2)] string MetadataUrl
);
