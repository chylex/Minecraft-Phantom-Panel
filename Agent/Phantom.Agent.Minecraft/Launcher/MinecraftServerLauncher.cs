using System.Diagnostics;
using System.Text;
using Phantom.Agent.Minecraft.Instance;
using Phantom.Agent.Minecraft.Java;

namespace Phantom.Agent.Minecraft.Launcher;

public abstract class MinecraftServerLauncher {
	private static Exception PropertyNotSet(string propertyName) {
		return new InvalidOperationException("Launcher property '" + propertyName + "' must be set!");
	}

	private readonly string instanceFolder;
	private readonly string serverJarPath;
	private readonly string jreFolder;

	private readonly uint initialHeapMegabytes;
	private readonly uint maximumHeapMegabytes;

	private protected MinecraftServerLauncher(MinecraftServerLaunchProperties properties) {
		this.jreFolder = properties.JreFolder ?? throw PropertyNotSet(nameof(properties.JreFolder));
		this.instanceFolder = properties.InstanceFolder ?? throw PropertyNotSet(nameof(properties.InstanceFolder));
		this.serverJarPath = properties.ServerJarPath ?? throw PropertyNotSet(nameof(properties.ServerJarPath));

		this.initialHeapMegabytes = properties.InitialHeapMegabytes ?? throw PropertyNotSet(nameof(properties.InitialHeapMegabytes));
		this.maximumHeapMegabytes = properties.MaximumHeapMegabytes ?? throw PropertyNotSet(nameof(properties.MaximumHeapMegabytes));
	}

	public InstanceSession Launch() {
		var startInfo = new ProcessStartInfo {
			FileName = jreFolder + "/bin/java",
			WorkingDirectory = instanceFolder,
			RedirectStandardInput = true,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			UseShellExecute = false,
			CreateNoWindow = false
		};

		var jvmArguments = new JvmArgumentBuilder {
			InitialHeapMegabytes = initialHeapMegabytes,
			MaximumHeapMegabytes = maximumHeapMegabytes
		};
		
		CustomizeJvmArguments(jvmArguments);
		
		var processArguments = startInfo.ArgumentList;
		jvmArguments.Build(processArguments);
		processArguments.Add("-jar");
		processArguments.Add(serverJarPath);
		processArguments.Add("nogui");

		var process = new Process { StartInfo = startInfo };
		var session = new InstanceSession(process);

		AcceptEula();

		process.Start();
		process.BeginOutputReadLine();
		process.BeginErrorReadLine();

		return session;
	}

	private protected virtual void CustomizeJvmArguments(JvmArgumentBuilder arguments) {}

	private void AcceptEula() {
		File.WriteAllLines(Path.Combine(instanceFolder, "eula.txt"), new [] { "# EULA", "eula=true" }, Encoding.UTF8);
	}
}
