using System.Text;
using Phantom.Agent.Minecraft.Instance;
using Phantom.Agent.Minecraft.Java;
using Phantom.Agent.Minecraft.Server;
using Phantom.Utils.Processes;
using Serilog;

namespace Phantom.Agent.Minecraft.Launcher;

public abstract class BaseLauncher : IServerLauncher {
	private readonly InstanceProperties instanceProperties;
	
	protected string MinecraftVersion => instanceProperties.ServerVersion;
	
	private protected BaseLauncher(InstanceProperties instanceProperties) {
		this.instanceProperties = instanceProperties;
	}
	
	public async Task<LaunchResult> Launch(ILogger logger, LaunchServices services, EventHandler<DownloadProgressEventArgs> downloadProgressEventHandler, CancellationToken cancellationToken) {
		if (!services.JavaRuntimeRepository.TryGetByGuid(instanceProperties.JavaRuntimeGuid, out var javaRuntimeExecutable)) {
			return new LaunchResult.InvalidJavaRuntime();
		}
		
		var vanillaServerJarPath = await services.ServerExecutables.DownloadAndGetPath(instanceProperties.LaunchProperties.ServerDownloadInfo, MinecraftVersion, downloadProgressEventHandler, cancellationToken);
		if (vanillaServerJarPath == null) {
			return new LaunchResult.CouldNotDownloadMinecraftServer();
		}
		
		ServerJarInfo? serverJar;
		try {
			serverJar = await PrepareServerJar(logger, vanillaServerJarPath, cancellationToken);
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
			await UpdateServerProperties(instanceProperties, cancellationToken);
		} catch (Exception e) {
			logger.Error(e, "Caught exception while configuring the server.");
			return new LaunchResult.CouldNotConfigureMinecraftServer();
		}
		
		var processConfigurator = new ProcessConfigurator {
			FileName = javaRuntimeExecutable.ExecutablePath,
			WorkingDirectory = instanceProperties.InstanceFolder,
			RedirectInput = true,
			UseShellExecute = false,
		};
		
		var processArguments = processConfigurator.ArgumentList;
		PrepareJvmArguments(serverJar).Build(processArguments);
		processArguments.Add("-jar");
		processArguments.Add(serverJar.FilePath);
		processArguments.Add("nogui");
		
		var process = processConfigurator.CreateProcess();
		var instanceProcess = new InstanceProcess(instanceProperties, process);
		
		try {
			process.Start();
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
	
	private JvmArgumentBuilder PrepareJvmArguments(ServerJarInfo serverJar) {
		var builder = new JvmArgumentBuilder(instanceProperties.JvmProperties);
		
		foreach (string argument in instanceProperties.JvmArguments) {
			builder.Add(argument);
		}
		
		foreach (var argument in serverJar.ExtraArgs) {
			builder.Add(argument);
		}
		
		CustomizeJvmArguments(builder);
		return builder;
	}
	
	private protected virtual void CustomizeJvmArguments(JvmArgumentBuilder arguments) {}
	
	private protected virtual Task<ServerJarInfo> PrepareServerJar(ILogger logger, string serverJarPath, CancellationToken cancellationToken) {
		return Task.FromResult(new ServerJarInfo(serverJarPath));
	}
	
	private static async Task AcceptEula(InstanceProperties instanceProperties) {
		var eulaFilePath = Path.Combine(instanceProperties.InstanceFolder, "eula.txt");
		await File.WriteAllLinesAsync(eulaFilePath, ["# EULA", "eula=true"], Encoding.UTF8);
	}
	
	private static async Task UpdateServerProperties(InstanceProperties instanceProperties, CancellationToken cancellationToken) {
		var serverPropertiesEditor = new JavaPropertiesFileEditor();
		instanceProperties.ServerProperties.SetTo(serverPropertiesEditor);
		await serverPropertiesEditor.EditOrCreate(Path.Combine(instanceProperties.InstanceFolder, "server.properties"), comment: "server.properties", cancellationToken);
	}
}
