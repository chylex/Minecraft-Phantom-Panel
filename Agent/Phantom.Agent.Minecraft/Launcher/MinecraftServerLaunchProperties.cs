using Phantom.Agent.Minecraft.Java;

namespace Phantom.Agent.Minecraft.Launcher; 

public sealed class MinecraftServerLaunchProperties {
	public JavaRuntime? JavaRuntime { get; init; }
	public string? InstanceFolder { get; init; }
	public string? ServerJarPath { get; init; }

	public uint? InitialHeapMegabytes { get; init; }
	public uint? MaximumHeapMegabytes { get; init; }

	public ushort? Port { get; init; }
}
