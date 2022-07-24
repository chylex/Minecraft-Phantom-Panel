using System.Diagnostics;
using System.Text;
using Kajabity.Tools.Java;
using Phantom.Agent.Minecraft.Instance;
using Phantom.Agent.Minecraft.Java;

namespace Phantom.Agent.Minecraft.Launcher;

public abstract class BaseLauncher {
	private readonly InstanceProperties instanceProperties;

	private protected BaseLauncher(InstanceProperties instanceProperties) {
		this.instanceProperties = instanceProperties;
	}

	public InstanceSession Launch() {
		var startInfo = new ProcessStartInfo {
			FileName = instanceProperties.JavaRuntime.JavaExecutablePath,
			WorkingDirectory = instanceProperties.InstanceFolder,
			RedirectStandardInput = true,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			UseShellExecute = false,
			CreateNoWindow = false
		};

		var jvmArguments = new JvmArgumentBuilder(instanceProperties.JvmProperties);
		CustomizeJvmArguments(jvmArguments);

		var processArguments = startInfo.ArgumentList;
		jvmArguments.Build(processArguments);
		processArguments.Add("-jar");
		processArguments.Add(instanceProperties.ServerJarPath);
		processArguments.Add("nogui");

		var process = new Process { StartInfo = startInfo };
		var session = new InstanceSession(process);

		AcceptEula(instanceProperties);
		UpdateServerProperties(instanceProperties);

		process.Start();
		process.BeginOutputReadLine();
		process.BeginErrorReadLine();

		return session;
	}

	private protected virtual void CustomizeJvmArguments(JvmArgumentBuilder arguments) {}

	private static void AcceptEula(InstanceProperties instanceProperties) {
		var eulaFilePath = Path.Combine(instanceProperties.InstanceFolder, "eula.txt");
		File.WriteAllLines(eulaFilePath, new [] { "# EULA", "eula=true" }, Encoding.UTF8);
	}

	private static void UpdateServerProperties(InstanceProperties instanceProperties) {
		var serverPropertiesFilePath = Path.Combine(instanceProperties.InstanceFolder, "server.properties");
		var serverPropertiesData = new JavaProperties();

		try {
			using var readStream = new FileStream(serverPropertiesFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
			serverPropertiesData.Load(readStream);
		} catch (FileNotFoundException) {
			// ignore
		}
		
		instanceProperties.ServerProperties.SetTo(serverPropertiesData);

		using var writeStream = new FileStream(serverPropertiesFilePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None);
		serverPropertiesData.Store(writeStream, true);
	}
}
