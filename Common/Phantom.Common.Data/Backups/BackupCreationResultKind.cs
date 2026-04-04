namespace Phantom.Common.Data.Backups;

public enum BackupCreationResultKind : byte {
	UnknownError = 0,
	Success = 1,
	InstanceNotRunning = 2,
	BackupTimedOut = 3,
	BackupCancelled = 4,
	BackupAlreadyRunning = 5,
	BackupFileAlreadyExists = 6,
	CouldNotCreateBackupDirectory = 7,
	CouldNotCopyInstanceIntoTemporaryDirectory = 8,
	CouldNotCreateBackupArchive = 9,
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
