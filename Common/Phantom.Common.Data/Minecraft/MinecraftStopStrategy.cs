using MemoryPack;

namespace Phantom.Common.Data.Minecraft;

[MemoryPackable(GenerateType.VersionTolerant)]
public readonly partial record struct MinecraftStopStrategy(
	[property: MemoryPackOrder(0)] ushort Seconds
) {
	public static MinecraftStopStrategy Instant => new (0);
}
