using System.Buffers;
using System.Collections.Immutable;
using Phantom.Agent.Services.Java;
using Phantom.Common.Data.Agent.Instance;

namespace Phantom.Agent.Services.Instances.Launch;

sealed class InstancePathResolver(AgentFolders agentFolders, JavaRuntimeRepository javaRuntimeRepository, InstanceProperties instanceProperties) : IInstancePathResolver {
	public string? Global(ImmutableArray<string> segments) {
		return ValidateAndCombinePath(agentFolders.ServerExecutableFolderPath, segments);
	}
	
	public string? Local(ImmutableArray<string> segments) {
		return ValidateAndCombinePath(instanceProperties.InstanceFolder, segments);
	}
	
	public string? Runtime(Guid guid) {
		return javaRuntimeRepository.TryGetByGuid(guid, out var runtime) ? runtime.ExecutablePath : null;
	}
	
	private static readonly SearchValues<char> InvalidPathPartChars = SearchValues.Create([
		Path.DirectorySeparatorChar,
		Path.AltDirectorySeparatorChar,
		..Path.GetInvalidPathChars(),
	]);
	
	private string? ValidateAndCombinePath(string basePath, ImmutableArray<string> pathSegments) {
		string path = basePath;
		
		foreach (string segment in pathSegments) {
			if (segment == "." || segment == ".." || segment.ContainsAny(InvalidPathPartChars) || Path.IsPathRooted(segment)) {
				return null;
			}
			
			path = Path.Combine(path, segment);
		}
		
		return Path.GetFullPath(path);
	}
}
