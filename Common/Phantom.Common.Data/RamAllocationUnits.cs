using System.Diagnostics.CodeAnalysis;
using MessagePack;

namespace Phantom.Common.Data;

/// <summary>
/// Represents a number of RAM allocation units, using the conversion factor of 256 MB per unit. Supports allocations from 256 MB (0 units) to 16 TB (65535 units).
/// </summary>
[MessagePackObject]
[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
public readonly record struct RamAllocationUnits(
	[property: Key(0)] ushort RawValue
) {
	[IgnoreMember]
	public uint InMegabytes => (uint) (RawValue + 1) * MegabytesPerUnit;

	private const int MegabytesPerUnit = 256;
	
	private const ushort MaximumUnits = ushort.MaxValue;
	private const int MaximumMegabytes = (MaximumUnits + 1) * MegabytesPerUnit;

	/// <summary>
	/// Converts an amount of <paramref name="megabytes"/> to <see cref="RamAllocationUnits"/>.
	/// </summary>
	/// <exception cref="ArgumentOutOfRangeException">If the value of <paramref name="megabytes"/> is not positive, not a multiple of <see cref="MegabytesPerUnit"/>, or exceeds the hard limit.</exception>
	public static RamAllocationUnits FromMegabytes(int megabytes) {
		if (megabytes % MegabytesPerUnit != 0) {
			throw new ArgumentOutOfRangeException(nameof(megabytes), "Must be a multiple of " + MegabytesPerUnit + " MB.");
		}

		long units = ((long) megabytes - MegabytesPerUnit) / MegabytesPerUnit;
		if (units < 0) {
			throw new ArgumentOutOfRangeException(nameof(megabytes), "Must be at least " + MegabytesPerUnit + " MB.");
		}
		else if (units > MaximumUnits) {
			throw new ArgumentOutOfRangeException(nameof(megabytes), "Must be at most " + MaximumMegabytes + " MB.");
		}
		
		return new RamAllocationUnits((ushort) Math.Clamp(units, 0, MaximumUnits));
	}

	/// <summary>
	/// Converts a string in the format &lt;number&gt;{M|G} (case-insensitive) to <see cref="RamAllocationUnits"/>.
	/// </summary>
	/// <exception cref="ArgumentOutOfRangeException">If the <paramref name="definition"/> is in the incorrect format, or the value cannot be converted via <see cref="FromMegabytes"/>.</exception>
	public static RamAllocationUnits FromString(ReadOnlySpan<char> definition) {
		if (definition.IsEmpty) {
			throw new ArgumentOutOfRangeException(nameof(definition), "Must not be empty.");
		}

		int unitMultiplier = char.ToUpperInvariant(definition[^1]) switch {
			'M' => 1,
			'G' => 1024,
			_ => throw new ArgumentOutOfRangeException(nameof(definition), "Must end with 'M' or 'G'.")
		};

		if (!int.TryParse(definition[..^1], out int size)) {
			throw new ArgumentOutOfRangeException(nameof(definition), "Must begin with a number.");
		}
		
		return FromMegabytes(size * unitMultiplier);
	}
}
