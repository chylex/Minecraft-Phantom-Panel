using System.Diagnostics;
using System.Text;
using Kajabity.Tools.Java;
using Phantom.Agent.Minecraft.Instance;
using Phantom.Agent.Minecraft.Java;
using Phantom.Agent.Minecraft.Server;
using Phantom.Common.Minecraft;
using Serilog;

namespace Phantom.Agent.Minecraft.Launcher;

public abstract class BaseLauncher {
	private readonly InstanceProperties instanceProperties;
	
	protected string MinecraftVersion => instanceProperties.ServerVersion;

	private protected BaseLauncher(InstanceProperties instanceProperties) {
		this.instanceProperties = instanceProperties;
	}

	public async Task<LaunchResult> Launch(ILogger logger, LaunchServices services, EventHandler<DownloadProgressEventArgs> downloadProgressEventHandler, CancellationToken cancellationToken) {
		if (!services.JavaRuntimeRepository.TryGetByGuid(instanceProperties.JavaRuntimeGuid, out var javaRuntimeExecutable)) {
			return new LaunchResult.InvalidJavaRuntime();
		}

		if (JvmArgumentsHelper.Validate(instanceProperties.JvmArguments) != null) {
			return new LaunchResult.InvalidJvmArguments();
		}

		var vanillaServerJarPath = await services.ServerExecutables.DownloadAndGetPath(instanceProperties.LaunchProperties.ServerDownloadInfo, MinecraftVersion, downloadProgressEventHandler, cancellationToken);
		if (vanillaServerJarPath == null) {
			return new LaunchResult.CouldNotDownloadMinecraftServer();
		}

		ServerJarInfo? serverJar;
		try {
			serverJar = await PrepareServerJar(logger, vanillaServerJarPath, instanceProperties.InstanceFolder, cancellationToken);
		} catch (OperationCanceledException) {
			throw;
		} catch (Exception e) {
			logger.Error(e, "Caught exception while preparing the server jar.");
			return new LaunchResult.CouldNotPrepareMinecraftServerLauncher();
		}

		if (!File.Exists(serverJar.FilePath)) {
			logger.Error("Missing prepared server or launcher jar: {FilePath}", serverJar.FilePath);
			return new LaunchResult.CouldNotPrepareMinecraftServerLauncher();
		}

		try {
			await AcceptEula(instanceProperties);
			await UpdateServerProperties(instanceProperties);
		} catch (Exception e) {
			logger.Error(e, "Caught exception while configuring the server.");
			return new LaunchResult.CouldNotConfigureMinecraftServer();
		}
		
		var startInfo = new ProcessStartInfo {
			FileName = javaRuntimeExecutable.ExecutablePath,
			WorkingDirectory = instanceProperties.InstanceFolder,
			RedirectStandardInput = true,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			UseShellExecute = false,
			CreateNoWindow = false
		};
		
		var jvmArguments = new JvmArgumentBuilder(instanceProperties.JvmProperties, instanceProperties.JvmArguments);
		CustomizeJvmArguments(jvmArguments);

		var processArguments = startInfo.ArgumentList;
		jvmArguments.Build(processArguments);
		
		foreach (var extraArgument in serverJar.ExtraArgs) {
			processArguments.Add(extraArgument);
		}
		
		processArguments.Add("-jar");
		processArguments.Add(serverJar.FilePath);
		processArguments.Add("nogui");

		var process = new Process { StartInfo = startInfo };
		var instanceProcess = new InstanceProcess(instanceProperties, process);

		try {
			process.Start();
			process.BeginOutputReadLine();
			process.BeginErrorReadLine();
		} catch (Exception launchException) {
			logger.Error(launchException, "Caught exception launching the server process.");
			
			try {
				process.Kill();
			} catch (Exception killException) {
				logger.Error(killException, "Caught exception trying to kill the server process after a failed launch.");
			}

			return new LaunchResult.CouldNotStartMinecraftServer();
		}

		return new LaunchResult.Success(instanceProcess);
	}

	private protected virtual void CustomizeJvmArguments(JvmArgumentBuilder arguments) {}

	private protected virtual Task<ServerJarInfo> PrepareServerJar(ILogger logger, string serverJarPath, string instanceFolderPath, CancellationToken cancellationToken) {
		return Task.FromResult(new ServerJarInfo(serverJarPath));
	}

	private static async Task AcceptEula(InstanceProperties instanceProperties) {
		var eulaFilePath = Path.Combine(instanceProperties.InstanceFolder, "eula.txt");
		await File.WriteAllLinesAsync(eulaFilePath, new [] { "# EULA", "eula=true" }, Encoding.UTF8);
	}

	private static async Task UpdateServerProperties(InstanceProperties instanceProperties) {
		var serverPropertiesFilePath = Path.Combine(instanceProperties.InstanceFolder, "server.properties");
		var serverPropertiesData = new JavaProperties();

		await using var fileStream = new FileStream(serverPropertiesFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
		try {
			serverPropertiesData.Load(fileStream);
		} catch (ParseException e) {
			throw new Exception("Could not parse server.properties file: " + serverPropertiesFilePath, e);
		}
		
		instanceProperties.ServerProperties.SetTo(serverPropertiesData);

		fileStream.Seek(0L, SeekOrigin.Begin);
		fileStream.SetLength(0L);
		
		serverPropertiesData.Store(fileStream, true);
	}
}
