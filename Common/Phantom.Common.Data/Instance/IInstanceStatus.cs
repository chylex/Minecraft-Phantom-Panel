using MemoryPack;

namespace Phantom.Common.Data.Instance;

[MemoryPackable]
[MemoryPackUnion(0, typeof(InstanceIsOffline))]
[MemoryPackUnion(1, typeof(InstanceIsInvalid))]
[MemoryPackUnion(2, typeof(InstanceIsNotRunning))]
[MemoryPackUnion(3, typeof(InstanceIsDownloading))]
[MemoryPackUnion(4, typeof(InstanceIsLaunching))]
[MemoryPackUnion(5, typeof(InstanceIsRunning))]
[MemoryPackUnion(6, typeof(InstanceIsRestarting))]
[MemoryPackUnion(7, typeof(InstanceIsStopping))]
[MemoryPackUnion(8, typeof(InstanceIsFailed))]
public partial interface IInstanceStatus {}

[MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record InstanceIsOffline : IInstanceStatus;

[MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record InstanceIsInvalid([property: MemoryPackOrder(0)] string Reason) : IInstanceStatus;

[MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record InstanceIsNotRunning : IInstanceStatus;

[MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record InstanceIsDownloading([property: MemoryPackOrder(0)] byte Progress) : IInstanceStatus;

[MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record InstanceIsLaunching : IInstanceStatus;

[MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record InstanceIsRunning : IInstanceStatus;

[MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record InstanceIsRestarting : IInstanceStatus;

[MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record InstanceIsStopping : IInstanceStatus;

[MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record InstanceIsFailed([property: MemoryPackOrder(0)] InstanceLaunchFailReason Reason) : IInstanceStatus;

public static class InstanceStatus {
	public static readonly IInstanceStatus Offline = new InstanceIsOffline();
	public static readonly IInstanceStatus NotRunning = new InstanceIsNotRunning();
	public static readonly IInstanceStatus Launching = new InstanceIsLaunching();
	public static readonly IInstanceStatus Running = new InstanceIsRunning();
	public static readonly IInstanceStatus Restarting = new InstanceIsRestarting();
	public static readonly IInstanceStatus Stopping = new InstanceIsStopping();
	
	public static IInstanceStatus Invalid(string reason) => new InstanceIsInvalid(reason);
	public static IInstanceStatus Downloading(byte progress) => new InstanceIsDownloading(progress);
	public static IInstanceStatus Failed(InstanceLaunchFailReason reason) => new InstanceIsFailed(reason);

	public static bool IsLaunching(this IInstanceStatus status) {
		return status is InstanceIsDownloading or InstanceIsLaunching or InstanceIsRestarting;
	}

	public static bool IsRunning(this IInstanceStatus status) {
		return status is InstanceIsRunning;
	}
	
	public static bool IsStopping(this IInstanceStatus status) {
		return status is InstanceIsStopping;
	}
	
	public static bool CanLaunch(this IInstanceStatus status) {
		return status is InstanceIsNotRunning or InstanceIsFailed;
	}

	public static bool CanStop(this IInstanceStatus status) {
		return status is InstanceIsDownloading or InstanceIsLaunching or InstanceIsRunning;
	}

	public static bool CanSendCommand(this IInstanceStatus status) {
		return status is InstanceIsRunning;
	}
}
