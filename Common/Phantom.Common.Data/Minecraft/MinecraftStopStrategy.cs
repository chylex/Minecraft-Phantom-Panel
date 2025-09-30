using MemoryPack;

namespace Phantom.Common.Data.Minecraft;

[MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record MinecraftStopStrategy(
	[property: MemoryPackOrder(0)] ushort Seconds
) {
	public static MinecraftStopStrategy Instant => new (0);
}
