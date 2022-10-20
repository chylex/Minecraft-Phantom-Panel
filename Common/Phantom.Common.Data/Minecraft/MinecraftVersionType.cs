using System.Collections.Immutable;

namespace Phantom.Common.Data.Minecraft;

public enum MinecraftVersionType : ushort {
	Other = 0,
	Release = 1,
	Snapshot = 2,
	OldBeta = 3,
	OldAlpha = 4
}

public static class MinecraftVersionTypes {
	public static readonly ImmutableArray<MinecraftVersionType> WithServerJars = ImmutableArray.Create(
		MinecraftVersionType.Release,
		MinecraftVersionType.Snapshot
	);

	public static MinecraftVersionType FromString(string? type) {
		return type switch {
			"release"   => MinecraftVersionType.Release,
			"snapshot"  => MinecraftVersionType.Snapshot,
			"old_beta"  => MinecraftVersionType.OldBeta,
			"old_alpha" => MinecraftVersionType.OldAlpha,
			_           => MinecraftVersionType.Other
		};
	}

	public static string ToNiceNamePlural(this MinecraftVersionType type) {
		return type switch {
			MinecraftVersionType.Release  => "Releases",
			MinecraftVersionType.Snapshot => "Snapshots",
			MinecraftVersionType.OldBeta  => "Beta",
			MinecraftVersionType.OldAlpha => "Alpha",
			_                             => "Unknown"
		};
	}
}
