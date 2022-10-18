using Phantom.Common.Data.Instance;

namespace Phantom.Server.Services.Instances; 

public sealed record Instance(
	InstanceConfiguration Configuration,
	IInstanceStatus Status
) {
	internal Instance(InstanceConfiguration configuration) : this(configuration, InstanceStatus.Offline) {}
}
