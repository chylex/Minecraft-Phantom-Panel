using Phantom.Agent.Minecraft.Java;
using Phantom.Agent.Minecraft.Server;

namespace Phantom.Agent.Minecraft.Launcher;

public sealed record LaunchServices(MinecraftServerExecutables ServerExecutables, JavaRuntimeRepository JavaRuntimeRepository);
