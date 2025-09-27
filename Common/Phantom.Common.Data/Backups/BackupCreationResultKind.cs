namespace Phantom.Common.Data.Backups;

public enum BackupCreationResultKind : byte {
	UnknownError = 0,
	Success = 1,
	InstanceNotRunning = 2,
	BackupTimedOut = 3,
	BackupCancelled = 4,
	BackupAlreadyRunning = 5,
	BackupFileAlreadyExists = 6,
	CouldNotCreateBackupFolder = 7,
	CouldNotCopyWorldToTemporaryFolder = 8,
	CouldNotCreateWorldArchive = 9,
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
