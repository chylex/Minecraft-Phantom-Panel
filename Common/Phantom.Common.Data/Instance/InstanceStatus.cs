using MessagePack;

namespace Phantom.Common.Data.Instance;

[Union(0, typeof(Offline))]
[Union(1, typeof(Invalid))]
[Union(2, typeof(NotRunning))]
[Union(3, typeof(Downloading))]
[Union(4, typeof(Launching))]
[Union(5, typeof(Running))]
[Union(6, typeof(Stopping))]
[Union(7, typeof(Failed))]
public abstract record InstanceStatus {
	public static readonly InstanceStatus IsOffline = new Offline();
	public static readonly InstanceStatus IsNotRunning = new NotRunning();
	public static readonly InstanceStatus IsLaunching = new Launching();
	public static readonly InstanceStatus IsRunning = new Running();
	public static readonly InstanceStatus IsStopping = new Stopping();

	[MessagePackObject]
	public sealed record Offline : InstanceStatus;

	[MessagePackObject]
	public sealed record Invalid(
		[property: Key(0)] string Reason
	) : InstanceStatus;

	[MessagePackObject]
	public sealed record NotRunning : InstanceStatus;

	[MessagePackObject]
	public sealed record Downloading(
		[property: Key(0)] byte Progress
	) : InstanceStatus;

	[MessagePackObject]
	public sealed record Launching : InstanceStatus;
	
	[MessagePackObject]
	public sealed record Running : InstanceStatus;

	[MessagePackObject]
	public sealed record Stopping : InstanceStatus;

	[MessagePackObject]
	public sealed record Failed(
		[property: Key(0)] InstanceLaunchFailReason Reason
	) : InstanceStatus;
}

public static class InstanceStatusExtensions {
	public static bool CanLaunch(this InstanceStatus status) {
		return status is InstanceStatus.NotRunning or InstanceStatus.Failed;
	}

	public static bool CanStop(this InstanceStatus status) {
		return status is InstanceStatus.Downloading or InstanceStatus.Launching or InstanceStatus.Running;
	}

	public static bool CanSendCommand(this InstanceStatus status) {
		return status is InstanceStatus.Running;
	}
}
