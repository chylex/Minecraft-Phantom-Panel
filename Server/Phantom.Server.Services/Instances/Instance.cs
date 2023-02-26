using Phantom.Common.Data.Instance;

namespace Phantom.Server.Services.Instances; 

public sealed record Instance(
	InstanceConfiguration Configuration,
	IInstanceStatus Status,
	bool LaunchAutomatically
) {
	internal Instance(InstanceConfiguration configuration, bool launchAutomatically = false) : this(configuration, InstanceStatus.Offline, launchAutomatically) {}
}
