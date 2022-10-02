using MessagePack;

namespace Phantom.Common.Data.Instance;

[Union(0, typeof(OfflineState))]
[Union(1, typeof(Invalid))]
[Union(2, typeof(NotRunningState))]
[Union(3, typeof(Downloading))]
[Union(4, typeof(RunningState))]
public abstract record InstanceState {
	public static readonly InstanceState Offline = new OfflineState();
	public static readonly InstanceState NotRunning = new NotRunningState();
	public static readonly InstanceState Running = new RunningState();
	
	[MessagePackObject]
	private sealed record OfflineState : InstanceState;

	[MessagePackObject]
	public sealed record Invalid(
		[property: Key(0)] string Reason
	) : InstanceState;

	[MessagePackObject]
	public sealed record NotRunningState : InstanceState;
	
	[MessagePackObject]
	public sealed record Downloading(
		[property: Key(0)] byte Progress
	) : InstanceState;
	
	[MessagePackObject]
	public sealed record RunningState : InstanceState;
}
