using System.Text;
using Phantom.Agent.Minecraft.Instance;

namespace Phantom.Agent.Minecraft.Launcher.Types; 

public class FabricLauncher : BaseLauncher {
	public FabricLauncher(InstanceProperties instanceProperties) : base(instanceProperties) {}
	
	private protected override async Task<string> PrepareServerJar(string serverJarPath, string instanceFolderPath, CancellationToken cancellationToken) {
		var launcherPropertiesFilePath = Path.Combine(instanceFolderPath, "fabric-server-launcher.properties");
		await File.WriteAllTextAsync(launcherPropertiesFilePath, "serverJar=" + serverJarPath, Encoding.UTF8, cancellationToken);
		
		// TODO
		return Path.Combine(instanceFolderPath, "fabric.jar");
	}
}
