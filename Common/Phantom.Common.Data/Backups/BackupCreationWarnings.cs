using System.Numerics;

namespace Phantom.Common.Data.Backups;

[Flags]
public enum BackupCreationWarnings : byte {
	None                             = 0,
	CouldNotDeleteTemporaryDirectory = 1 << 0,
	CouldNotCompressBackupArchive    = 1 << 1,
	SomeFilesChangedDuringBackup     = 1 << 2,
}

public static class BackupCreationWarningsExtensions {
	extension(BackupCreationWarnings warnings) {
		public int Count() {
			return BitOperations.PopCount((byte) warnings);
		}
		
		public IEnumerable<BackupCreationWarnings> ListFlags() {
			return Enum.GetValues<BackupCreationWarnings>().Where(warning => warning != BackupCreationWarnings.None && warnings.HasFlag(warning));
		}
	}
}
