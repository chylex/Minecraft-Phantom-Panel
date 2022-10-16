using Phantom.Agent.Minecraft.Java;
using Phantom.Agent.Minecraft.Server;
using Phantom.Utils.Runtime;

namespace Phantom.Agent.Minecraft.Launcher; 

public sealed record LaunchServices(TaskManager TaskManager, MinecraftServerExecutables ServerExecutables, JavaRuntimeRepository JavaRuntimeRepository);
