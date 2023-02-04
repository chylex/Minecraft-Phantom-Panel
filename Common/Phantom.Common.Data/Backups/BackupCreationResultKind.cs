namespace Phantom.Common.Data.Backups;

public enum BackupCreationResultKind : byte {
	UnknownError,
	Success,
	BackupCancelled,
	BackupAlreadyExists,
	CouldNotCreateBackupFolder,
	CouldNotCopyWorldToTemporaryFolder,
	CouldNotCreateWorldArchive
}

public static class BackupCreationResultSummaryExtensions {
	public static string ToSentence(this BackupCreationResultKind kind) {
		return kind switch {
			BackupCreationResultKind.Success                            => "Backup created successfully.",
			BackupCreationResultKind.BackupCancelled                    => "Backup cancelled.",
			BackupCreationResultKind.BackupAlreadyExists                => "Backup with the same name already exists.",
			BackupCreationResultKind.CouldNotCreateBackupFolder         => "Could not create backup folder.",
			BackupCreationResultKind.CouldNotCopyWorldToTemporaryFolder => "Could not copy world to temporary folder.",
			BackupCreationResultKind.CouldNotCreateWorldArchive         => "Could not create world archive.",
			_                                                              => "Unknown error."
		};
	}
}
