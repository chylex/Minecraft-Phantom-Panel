using System.Diagnostics;
using System.Text;
using Kajabity.Tools.Java;
using Phantom.Agent.Minecraft.Instance;
using Phantom.Agent.Minecraft.Java;
using Phantom.Agent.Minecraft.Server;

namespace Phantom.Agent.Minecraft.Launcher;

public abstract class BaseLauncher {
	private readonly MinecraftServerExecutables serverExecutables;
	private readonly InstanceProperties instanceProperties;

	private protected BaseLauncher(MinecraftServerExecutables serverExecutables, InstanceProperties instanceProperties) {
		this.serverExecutables = serverExecutables;
		this.instanceProperties = instanceProperties;
	}

	public async Task<InstanceSession> Launch(CancellationToken cancellationToken) {
		string serverJarPath;
		
		try {
			serverJarPath = await serverExecutables.DownloadAndGetPath(instanceProperties.ServerVersion, cancellationToken);
		} catch (Exception e) {
			throw e;
		}

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
		processArguments.Add(serverJarPath);
		processArguments.Add("nogui");

		var process = new Process { StartInfo = startInfo };
		var session = new InstanceSession(process);

		await AcceptEula(instanceProperties);
		await UpdateServerProperties(instanceProperties);

		process.Start();
		process.BeginOutputReadLine();
		process.BeginErrorReadLine();

		return session;
	}

	private protected virtual void CustomizeJvmArguments(JvmArgumentBuilder arguments) {}

	private static async Task AcceptEula(InstanceProperties instanceProperties) {
		var eulaFilePath = Path.Combine(instanceProperties.InstanceFolder, "eula.txt");
		await File.WriteAllLinesAsync(eulaFilePath, new [] { "# EULA", "eula=true" }, Encoding.UTF8);
	}

	private static async Task UpdateServerProperties(InstanceProperties instanceProperties) {
		var serverPropertiesFilePath = Path.Combine(instanceProperties.InstanceFolder, "server.properties");
		var serverPropertiesData = new JavaProperties();

		try {
			await using var readStream = new FileStream(serverPropertiesFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
			serverPropertiesData.Load(readStream);
		} catch (FileNotFoundException) {
			// ignore
		}
		
		instanceProperties.ServerProperties.SetTo(serverPropertiesData);

		await using var writeStream = new FileStream(serverPropertiesFilePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None);
		serverPropertiesData.Store(writeStream, true);
	}
}
