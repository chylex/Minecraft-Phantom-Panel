using Phantom.Agent.Minecraft.Instance;
using Phantom.Agent.Minecraft.Java;
using Phantom.Agent.Minecraft.Server;

namespace Phantom.Agent.Minecraft.Launcher; 

public class ForgeLauncher : BaseLauncher {
	public ForgeLauncher(MinecraftServerExecutables serverExecutables, InstanceProperties instanceProperties) : base(serverExecutables, instanceProperties) {}
	
	private protected override void CustomizeJvmArguments(JvmArgumentBuilder arguments) {
		arguments.AddProperty("terminal.ansi", "true"); // TODO
	}
}
