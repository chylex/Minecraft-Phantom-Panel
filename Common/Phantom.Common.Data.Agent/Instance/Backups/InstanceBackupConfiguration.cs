using MemoryPack;

namespace Phantom.Common.Data.Agent.Instance.Backups;

[MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record InstanceBackupConfiguration(
	[property: MemoryPackOrder(0)] InstanceBackupSchedule Schedule
);
