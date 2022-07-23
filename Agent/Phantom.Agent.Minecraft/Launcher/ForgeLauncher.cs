using Phantom.Agent.Minecraft.Java;

namespace Phantom.Agent.Minecraft.Launcher; 

public class ForgeLauncher : MinecraftServerLauncher {
	public ForgeLauncher(MinecraftServerLaunchProperties properties) : base(properties) {}
	
	private protected override void CustomizeJvmArguments(JvmArgumentBuilder arguments) {
		arguments.AddProperty("terminal.ansi", "true");
	}
}
