using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Phantom.Common.Data;
using Phantom.Common.Data.Agent;
using Phantom.Common.Data.Agent.Instance;
using Phantom.Common.Data.Agent.Instance.Launch;
using Phantom.Common.Data.Web.Instance;
using Phantom.Common.Data.Web.Minecraft;
using Phantom.Utils.Collections;

namespace Phantom.Controller.Minecraft;

public sealed partial class MinecraftLaunchRecipes(MinecraftVersions minecraftVersions) {
	public async Task<Result<InstanceLaunchRecipe, MinecraftLaunchRecipeCreationFailReason>> Create(InstanceConfiguration configuration, CancellationToken cancellationToken) {
		string minecraftVersion = configuration.MinecraftVersion;
		
		var serverExecutableInfo = await minecraftVersions.GetServerExecutableInfo(minecraftVersion, cancellationToken);
		if (serverExecutableInfo == null) {
			return MinecraftLaunchRecipeCreationFailReason.MinecraftVersionNotFound;
		}
		
		uint maximumHeapSizeMegabvtes = configuration.MemoryAllocation.InMegabytes;
		uint initialHeapSizeMegabytes = maximumHeapSizeMegabvtes / 2;
		
		var minecraftVersionPathSegment = SanitizePath(minecraftVersion);
		var vanillaJarFilePath = new InstancePath.Global(["minecraft", "vanilla", minecraftVersionPathSegment, "server.jar"]);
		
		var steps = ImmutableArray.CreateBuilder<IInstanceLaunchStep>();
		
		steps.Add(new InstanceLaunchStep.DownloadFile(serverExecutableInfo, vanillaJarFilePath));
		steps.Add(new InstanceLaunchStep.EditPropertiesFile(new InstancePath.Local(["eula.txt"]), "EULA", Eula));
		steps.Add(new InstanceLaunchStep.EditPropertiesFile(new InstancePath.Local(["server.properties"]), "Server Properties", ServerProperties(configuration)));
		
		var serverExecutableFilePath = vanillaJarFilePath;
		var additionalJvmArguments = new List<IInstanceValue>();
		
		if (configuration.MinecraftServerKind == MinecraftServerKind.Fabric) {
			var fabricLauncherFilePath = new InstancePath.Global(["minecraft", "fabric", minecraftVersionPathSegment, "loader.jar"]);
			AddFabricDownloadStep(steps, configuration, fabricLauncherFilePath);
			
			serverExecutableFilePath = fabricLauncherFilePath;
			
			additionalJvmArguments.Add(new InstanceValues.Concatenation([
				new InstanceValues.Text("-Dfabric.installer.server.gameJar="),
				new InstanceValues.Path(vanillaJarFilePath),
			]));
		}
		
		return new InstanceLaunchRecipe(steps.ToImmutable(), new InstancePath.Runtime(configuration.JavaRuntimeGuid), [
			..configuration.JvmArguments.Select(static arg => new InstanceValues.Text(arg)),
			..additionalJvmArguments,
			new InstanceValues.Text($"-Xms{initialHeapSizeMegabytes}M"),
			new InstanceValues.Text($"-Xmx{maximumHeapSizeMegabvtes}M"),
			new InstanceValues.Text("-jar"),
			new InstanceValues.Path(serverExecutableFilePath),
			new InstanceValues.Text("-nogui"),
		]);
	}
	
	[GeneratedRegex(@"[^a-zA-Z0-9_\-\.]", RegexOptions.Compiled)]
	private static partial Regex SanitizePathRegex();
	
	private static string SanitizePath(string path) {
		return SanitizePathRegex().IsMatch(path) ? SanitizePathRegex().Replace(path, "_") : path;
	}
	
	private static readonly ImmutableDictionary<string, string> Eula = ImmutableDictionary.From([
		("eula", "true"),
	]);
	
	private static ImmutableDictionary<string, string> ServerProperties(InstanceConfiguration configuration) {
		return ImmutableDictionary.From([
			("server-port", configuration.ServerPort.ToString()),
			("rcon.port", configuration.RconPort.ToString()),
			("enable-rcon", "true"),
			("sync-chunk-writes", "false"),
		]);
	}
	
	private static void AddFabricDownloadStep(ImmutableArray<IInstanceLaunchStep>.Builder steps, InstanceConfiguration configuration, InstancePath.Global filePath) {
		// TODO customizable loader version
		string minecraftVersion = configuration.MinecraftVersion;
		string fabricLauncherUrl = $"https://meta.fabricmc.net/v2/versions/loader/{minecraftVersion}/stable/stable/server/jar";
		steps.Add(new InstanceLaunchStep.DownloadFile(new FileDownloadInfo(fabricLauncherUrl), filePath));
	}
}
