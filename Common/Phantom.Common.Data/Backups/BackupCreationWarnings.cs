using System.Numerics;

namespace Phantom.Common.Data.Backups;

[Flags]
public enum BackupCreationWarnings : byte {
	None = 0,
	CouldNotDeleteTemporaryFolder = 1 << 0,
	CouldNotCompressWorldArchive = 1 << 1,
	CouldNotRestoreAutomaticSaving = 1 << 2
}

public static class BackupCreationWarningsExtensions {
	public static int Count(this BackupCreationWarnings warnings) {
		return BitOperations.PopCount((byte) warnings);
	}

	public static IEnumerable<BackupCreationWarnings> ListFlags(this BackupCreationWarnings warnings) {
		return Enum.GetValues<BackupCreationWarnings>().Where(warning => warning != BackupCreationWarnings.None && warnings.HasFlag(warning));
	}
}
