using MessagePack;

namespace Phantom.Common.Data.Minecraft;

[MessagePackObject]
public readonly record struct MinecraftStopStrategy(
	[property: Key(0)] ushort Seconds
) {
	public static MinecraftStopStrategy Instant => new (0);
}
