using MemoryPack;
using Phantom.Common.Data.Instance;

namespace Phantom.Common.Data.Web.Instance;

[MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record Instance(
	[property: MemoryPackOrder(0)] Guid InstanceGuid,
	[property: MemoryPackOrder(1)] InstanceConfiguration Configuration,
	[property: MemoryPackOrder(2)] IInstanceStatus Status,
	[property: MemoryPackOrder(3)] bool LaunchAutomatically
) {
	public static Instance Offline(Guid instanceGuid, InstanceConfiguration configuration, bool launchAutomatically = false) {
		return new Instance(instanceGuid, configuration, InstanceStatus.Offline, launchAutomatically);
	}
}
