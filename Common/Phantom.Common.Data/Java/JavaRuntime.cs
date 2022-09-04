using MessagePack;

namespace Phantom.Common.Data.Java; 

[MessagePackObject]
public sealed record JavaRuntime(
	[property: Key(0)] string MainVersion,
	[property: Key(1)] string FullVersion,
	[property: Key(2)] string Vendor
) : IComparable<JavaRuntime> {
	public int CompareTo(JavaRuntime? other) {
		if (ReferenceEquals(this, other)) {
			return 0;
		}

		if (ReferenceEquals(null, other)) {
			return 1;
		}

		if (Version.TryParse(FullVersion, out var fullVersion) && Version.TryParse(other.FullVersion, out var otherFullVersion)) {
			var versionComparison = -fullVersion.CompareTo(otherFullVersion);
			if (versionComparison != 0) {
				return versionComparison;
			}
		}

		return string.Compare(Vendor, other.Vendor, StringComparison.OrdinalIgnoreCase);
	}
}
