using MessagePack;

namespace Phantom.Common.Data;

/// <summary>
/// Represents a number of RAM allocation units, using the conversion factor of 256 MB per unit. Supports allocations from 256 MB (0 units) to 16 TB (65535 units).
/// </summary>
[MessagePackObject]
public readonly record struct RamAllocationUnits {
	private const int ConversionFactorMegabytes = 256;

	public static RamAllocationUnits FromMegabytesFloored(int megabytes) {
		if (megabytes < ConversionFactorMegabytes) {
			throw new ArgumentOutOfRangeException(nameof(megabytes), "Must be at least 256 MB.");
		}

		int units = (megabytes - ConversionFactorMegabytes) / ConversionFactorMegabytes;
		return new RamAllocationUnits((ushort) Math.Clamp(units, ushort.MinValue, ushort.MaxValue));
	}
	
	[Key(0)]
	private readonly ushort rawValue;
	
	[IgnoreMember]
	public uint InMegabytes => (uint) (rawValue + 1) * ConversionFactorMegabytes;

	private RamAllocationUnits(ushort rawValue) {
		this.rawValue = rawValue;
	}
}
