using Phantom.Agent.Minecraft.Instance;
using Phantom.Agent.Minecraft.Java;

namespace Phantom.Agent.Minecraft.Launcher; 

public class ForgeLauncher : BaseLauncher {
	public ForgeLauncher(InstanceProperties instanceProperties) : base(instanceProperties) {}
	
	private protected override void CustomizeJvmArguments(JvmArgumentBuilder arguments) {
		arguments.AddProperty("terminal.ansi", "true"); // TODO
	}
}
