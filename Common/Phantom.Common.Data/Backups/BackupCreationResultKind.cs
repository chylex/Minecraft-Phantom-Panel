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

	public static string ToSentence(this BackupCreationResultKind kind) {
		return kind switch {
			BackupCreationResultKind.Success                            => "Backup created successfully.",
			BackupCreationResultKind.InstanceNotRunning                 => "Instance is not running.",
			BackupCreationResultKind.BackupCancelled                    => "Backup cancelled.",
			BackupCreationResultKind.BackupAlreadyRunning               => "A backup is already being created.",
			BackupCreationResultKind.BackupFileAlreadyExists            => "Backup with the same name already exists.",
			BackupCreationResultKind.CouldNotCreateBackupFolder         => "Could not create backup folder.",
			BackupCreationResultKind.CouldNotCopyWorldToTemporaryFolder => "Could not copy world to temporary folder.",
			BackupCreationResultKind.CouldNotCreateWorldArchive         => "Could not create world archive.",
			_                                                           => "Unknown error."
		};
	}
}
