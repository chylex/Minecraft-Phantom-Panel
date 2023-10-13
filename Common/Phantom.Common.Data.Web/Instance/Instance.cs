using MemoryPack;
using Phantom.Common.Data.Instance;

namespace Phantom.Common.Data.Web.Instance;

[MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record Instance(
	[property: MemoryPackOrder(0)] InstanceConfiguration Configuration,
	[property: MemoryPackOrder(1)] IInstanceStatus Status,
	[property: MemoryPackOrder(2)] bool LaunchAutomatically
) {
	public static Instance Offline(InstanceConfiguration configuration, bool launchAutomatically = false) {
		return new Instance(configuration, InstanceStatus.Offline, launchAutomatically);
	}
}
