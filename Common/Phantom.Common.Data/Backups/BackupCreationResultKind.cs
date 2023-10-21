namespace Phantom.Common.Data.Backups;

public enum BackupCreationResultKind : byte {
	UnknownError,
	Success,
	InstanceNotRunning,
	BackupCancelled,
	BackupAlreadyRunning,
	BackupFileAlreadyExists,
	CouldNotCreateBackupFolder,
	CouldNotCopyWorldToTemporaryFolder,
	CouldNotCreateWorldArchive
}

public static class BackupCreationResultSummaryExtensions {
	public static bool ShouldRetry(this BackupCreationResultKind kind) {
		return kind != BackupCreationResultKind.Success &&
		       kind != BackupCreationResultKind.InstanceNotRunning &&
		       kind != BackupCreationResultKind.BackupCancelled &&
		       kind != BackupCreationResultKind.BackupAlreadyRunning &&
		       kind != BackupCreationResultKind.BackupFileAlreadyExists;
	}
}
