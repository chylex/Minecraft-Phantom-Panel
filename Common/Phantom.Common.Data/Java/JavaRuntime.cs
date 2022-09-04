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

		var fullVersionComparison = string.Compare(FullVersion, other.FullVersion, StringComparison.OrdinalIgnoreCase);
		if (fullVersionComparison != 0) {
			return -fullVersionComparison;
		}

		// TODO not proper version comparison
		var mainVersionComparison = string.Compare(MainVersion, other.MainVersion, StringComparison.OrdinalIgnoreCase);
		if (mainVersionComparison != 0) {
			return -mainVersionComparison;
		}

		return string.Compare(Vendor, other.Vendor, StringComparison.OrdinalIgnoreCase);
	}
}
