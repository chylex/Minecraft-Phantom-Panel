using MemoryPack;

namespace Phantom.Common.Data.Backups;

[MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record BackupCreationResult(
	[property: MemoryPackOrder(0)] BackupCreationResultKind Kind,
	[property: MemoryPackOrder(1)] BackupCreationWarnings Warnings = BackupCreationWarnings.None
) {
	public sealed class Builder {
		public BackupCreationResultKind Kind { get; set; } = BackupCreationResultKind.Success;
		public BackupCreationWarnings Warnings { get; set; }
		
		public BackupCreationResult Build() {
			return new BackupCreationResult(Kind, Warnings);
		}
	}
}
