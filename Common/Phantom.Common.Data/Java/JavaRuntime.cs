using System.Diagnostics.CodeAnalysis;
using MemoryPack;

namespace Phantom.Common.Data.Java;

[MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record JavaRuntime(
	[property: MemoryPackOrder(0)] string MainVersion,
	[property: MemoryPackOrder(1)] string FullVersion,
	[property: MemoryPackOrder(2)] string DisplayName
) : IComparable<JavaRuntime> {
	public int CompareTo(JavaRuntime? other) {
		if (ReferenceEquals(this, other)) {
			return 0;
		}
		
		if (other is null) {
			return 1;
		}
		
		if (TryParseFullVersion(FullVersion, out var fullVersion) && TryParseFullVersion(other.FullVersion, out var otherFullVersion)) {
			var versionComparison = -fullVersion.CompareTo(otherFullVersion);
			if (versionComparison != 0) {
				return versionComparison;
			}
		}
		
		return string.Compare(DisplayName, other.DisplayName, StringComparison.OrdinalIgnoreCase);
	}
	
	private static bool TryParseFullVersion(string versionString, [NotNullWhen(true)] out Version? version) {
		int dashIndex = versionString.IndexOf('-');
		var versionSpan = dashIndex != -1 ? versionString.AsSpan(start: 0, dashIndex) : versionString;
		if (versionSpan.Contains('_')) {
			versionSpan = versionSpan.ToString().Replace(oldChar: '_', newChar: '.');
		}
		
		return Version.TryParse(versionSpan, out version);
	}
}
