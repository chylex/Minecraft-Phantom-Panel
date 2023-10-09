using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Phantom.Common.Data;

namespace Phantom.Server.Database.Converters;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
sealed class RamAllocationUnitsConverter : ValueConverter<RamAllocationUnits, ushort> {
	public RamAllocationUnitsConverter() : base(
		static units => units.RawValue,
		static value => new RamAllocationUnits(value)
	) {}
}
