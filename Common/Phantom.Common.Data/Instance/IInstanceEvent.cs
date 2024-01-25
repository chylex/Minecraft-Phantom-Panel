using MemoryPack;
using Phantom.Common.Data.Backups;

namespace Phantom.Common.Data.Instance;

[MemoryPackable]
[MemoryPackUnion(0, typeof(InstanceLaunchSucceededEvent))]
[MemoryPackUnion(1, typeof(InstanceLaunchFailedEvent))]
[MemoryPackUnion(2, typeof(InstanceCrashedEvent))]
[MemoryPackUnion(3, typeof(InstanceStoppedEvent))]
[MemoryPackUnion(4, typeof(InstanceBackupCompletedEvent))]
public partial interface IInstanceEvent {
	void Accept(IInstanceEventVisitor visitor);
}

[MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record InstanceLaunchSucceededEvent : IInstanceEvent {
	public void Accept(IInstanceEventVisitor visitor) {
		visitor.OnLaunchSucceeded(this);
	}
}

[MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record InstanceLaunchFailedEvent([property: MemoryPackOrder(0)] InstanceLaunchFailReason Reason) : IInstanceEvent {
	public void Accept(IInstanceEventVisitor visitor) {
		visitor.OnLaunchFailed(this);
	}
}

[MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record InstanceCrashedEvent : IInstanceEvent {
	public void Accept(IInstanceEventVisitor visitor) {
		visitor.OnCrashed(this);
	}
}

[MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record InstanceStoppedEvent : IInstanceEvent {
	public void Accept(IInstanceEventVisitor visitor) {
		visitor.OnStopped(this);
	}
}

[MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record InstanceBackupCompletedEvent([property: MemoryPackOrder(0)] BackupCreationResultKind Kind, [property: MemoryPackOrder(1)] BackupCreationWarnings Warnings) : IInstanceEvent {
	public void Accept(IInstanceEventVisitor visitor) {
		visitor.OnBackupCompleted(this);
	}
}

public static class InstanceEvent {
	public static readonly IInstanceEvent LaunchSucceeded = new InstanceLaunchSucceededEvent();
	public static readonly IInstanceEvent Crashed = new InstanceCrashedEvent();
	public static readonly IInstanceEvent Stopped = new InstanceStoppedEvent();
}
