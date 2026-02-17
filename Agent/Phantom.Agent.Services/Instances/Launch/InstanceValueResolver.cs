using Phantom.Common.Data.Agent.Instance;

namespace Phantom.Agent.Services.Instances.Launch;

sealed class InstanceValueResolver(IInstancePathResolver pathResolver) : IInstanceValueResolver {
	public string? Path(IInstancePath value) {
		return value.Resolve(pathResolver);
	}
}
