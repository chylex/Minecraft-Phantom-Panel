using System.Collections.ObjectModel;
using Phantom.Agent.Minecraft.Instance;
using Phantom.Agent.Minecraft.Java;
using Serilog;

namespace Phantom.Agent.Minecraft.Launcher.Types; 

public sealed class ForgeLauncher : BaseLauncher {
	public ForgeLauncher(InstanceProperties instanceProperties) : base(instanceProperties) {}
	
	private protected override void CustomizeJvmArguments(JvmArgumentBuilder arguments) {
		arguments.AddProperty("terminal.ansi", "true"); // TODO
	}

	protected override void PrepareJavaProcessArguments(Collection<string> processArguments, string serverJarFilePath) {
		if (OperatingSystem.IsWindows()) {
			processArguments.Add("@libraries/net/minecraftforge/forge/1.20.1-47.2.0/win_args.txt");
		}
		else {
			processArguments.Add("@libraries/net/minecraftforge/forge/1.20.1-47.2.0/unix_args.txt");
		}
		
		processArguments.Add("nogui");
	}

	private protected override Task<ServerJarInfo> PrepareServerJar(ILogger logger, string serverJarPath, CancellationToken cancellationToken) {
		return Task.FromResult(new ServerJarInfo(Path.Combine(InstanceFolder, "run.sh")));
	}
}
