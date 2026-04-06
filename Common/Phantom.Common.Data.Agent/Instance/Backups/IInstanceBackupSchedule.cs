using MemoryPack;

namespace Phantom.Common.Data.Agent.Instance.Backups;

[MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record InstanceBackupSchedule(
	[property: MemoryPackOrder(0)] ushort InitialDelayInMinutes,
	[property: MemoryPackOrder(1)] ushort BackupIntervalInMinutes,
	[property: MemoryPackOrder(2)] ushort BackupFailureRetryDelayInMinutes,
	[property: MemoryPackOrder(3)] Optional<IInstancePlayerCountDetectionStrategy> PlayerCountDetectionStrategy
) {
	[MemoryPackIgnore]
	public TimeSpan InitialDelay => TimeSpan.FromMinutes(InitialDelayInMinutes);
	
	[MemoryPackIgnore]
	public TimeSpan BackupInterval => TimeSpan.FromMinutes(AtLeastOne(BackupIntervalInMinutes));
	
	[MemoryPackIgnore]
	public TimeSpan BackupFailureRetryDelay => TimeSpan.FromMinutes(AtLeastOne(BackupFailureRetryDelayInMinutes));
	
	private static ushort AtLeastOne(ushort value) {
		return Math.Max(value, (ushort) 1);
	}
}
