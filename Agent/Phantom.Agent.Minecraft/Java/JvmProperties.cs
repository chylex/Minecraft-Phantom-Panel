namespace Phantom.Agent.Minecraft.Java;

public sealed record JvmProperties(
	uint InitialHeapMegabytes,
	uint MaximumHeapMegabytes
);
