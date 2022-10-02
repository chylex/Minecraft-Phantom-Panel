using Phantom.Common.Data.Instance;

namespace Phantom.Server.Services.Instances; 

public sealed record Instance(
	InstanceConfiguration Configuration,
	InstanceState State
) {
	internal Instance(InstanceConfiguration configuration) : this(configuration, InstanceState.Offline) {}
}
