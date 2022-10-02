using MessagePack;

namespace Phantom.Common.Data.Instance;

[Union(0, typeof(Offline))]
[Union(1, typeof(Invalid))]
[Union(2, typeof(NotRunning))]
[Union(3, typeof(Downloading))]
[Union(4, typeof(Running))]
[Union(5, typeof(Stopping))]
[Union(6, typeof(Failed))]
public abstract record InstanceStatus {
	public static readonly InstanceStatus IsOffline = new Offline();
	public static readonly InstanceStatus IsNotRunning = new NotRunning();
	public static readonly InstanceStatus IsRunning = new Running();
	public static readonly InstanceStatus IsStopping = new Stopping();
	
	[MessagePackObject]
	private sealed record Offline : InstanceStatus;

	[MessagePackObject]
	public sealed record Invalid(
		[property: Key(0)] string Reason
	) : InstanceStatus;

	[MessagePackObject]
	private sealed record NotRunning : InstanceStatus;
	
	[MessagePackObject]
	public sealed record Downloading(
		[property: Key(0)] byte Progress
	) : InstanceStatus;
	
	[MessagePackObject]
	private sealed record Running : InstanceStatus;
	
	[MessagePackObject]
	private sealed record Stopping : InstanceStatus;
	
	[MessagePackObject]
	public sealed record Failed(
		[property: Key(0)] InstanceLaunchFailReason Reason
	) : InstanceStatus;
}
