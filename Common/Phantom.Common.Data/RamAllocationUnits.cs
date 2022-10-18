using System.Diagnostics.CodeAnalysis;
using MemoryPack;

namespace Phantom.Common.Data;

/// <summary>
/// Represents a number of RAM allocation units, using the conversion factor of 256 MB per unit. Supports allocations up to 16 TB minus 256 MB (65535 units).
/// </summary>
[MemoryPackable]
[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
public readonly partial record struct RamAllocationUnits(
	[property: MemoryPackOrder(0)] ushort RawValue
) : IComparable<RamAllocationUnits> {
	[MemoryPackIgnore]
	public uint InMegabytes => (uint) RawValue * MegabytesPerUnit;

	public int CompareTo(RamAllocationUnits other) {
		return RawValue.CompareTo(other.RawValue);
	}

	public static RamAllocationUnits operator +(RamAllocationUnits left, RamAllocationUnits right) {
		ushort units = (ushort) Math.Min(left.RawValue + right.RawValue, MaximumUnits);
		return new RamAllocationUnits(units);
	}
	
	public static RamAllocationUnits operator -(RamAllocationUnits left, RamAllocationUnits right) {
		ushort units = (ushort) Math.Max(left.RawValue - right.RawValue, 0);
		return new RamAllocationUnits(units);
	}

	public static bool operator <(RamAllocationUnits left, RamAllocationUnits right) {
		return left.CompareTo(right) < 0;
	}

	public static bool operator >(RamAllocationUnits left, RamAllocationUnits right) {
		return left.CompareTo(right) > 0;
	}

	public static bool operator <=(RamAllocationUnits left, RamAllocationUnits right) {
		return left.CompareTo(right) <= 0;
	}

	public static bool operator >=(RamAllocationUnits left, RamAllocationUnits right) {
		return left.CompareTo(right) >= 0;
	}

	private const int MegabytesPerUnit = 256;
	
	public const ushort MaximumUnits = ushort.MaxValue;
	private const int MaximumMegabytes = MaximumUnits * MegabytesPerUnit;
	
	public static readonly RamAllocationUnits Zero = new (0);

	/// <summary>
	/// Converts an amount of <paramref name="megabytes"/> to <see cref="RamAllocationUnits"/>.
	/// </summary>
	/// <exception cref="ArgumentOutOfRangeException">If the value of <paramref name="megabytes"/> is negative, not a multiple of <see cref="MegabytesPerUnit"/>, or exceeds the hard limit.</exception>
	public static RamAllocationUnits FromMegabytes(int megabytes) {
		if (megabytes % MegabytesPerUnit != 0) {
			throw new ArgumentOutOfRangeException(nameof(megabytes), "Must be a multiple of " + MegabytesPerUnit + " MB.");
		}

		long units = (long) megabytes / MegabytesPerUnit;
		if (units < 0) {
			throw new ArgumentOutOfRangeException(nameof(megabytes), "Must be at least 0 MB.");
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

	/// <summary>
	/// Converts a string in the format &lt;number&gt;{M|G} (case-insensitive) to <see cref="RamAllocationUnits"/>.
	/// </summary>
	/// <exception cref="ArgumentOutOfRangeException">If the <paramref name="definition"/> is in the incorrect format, or the value cannot be converted via <see cref="FromMegabytes"/>.</exception>
	public static RamAllocationUnits FromString(string definition) {
		return FromString((ReadOnlySpan<char>) definition);
	}
}
