using Phantom.Agent.Minecraft.Instance;
using Phantom.Agent.Minecraft.Server;

namespace Phantom.Agent.Minecraft.Launcher; 

public sealed class VanillaLauncher : BaseLauncher {
	public VanillaLauncher(MinecraftServerExecutables serverExecutables, InstanceProperties instanceProperties) : base(serverExecutables, instanceProperties) {}
}
