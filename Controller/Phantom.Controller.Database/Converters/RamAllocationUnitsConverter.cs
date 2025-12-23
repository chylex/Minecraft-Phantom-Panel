using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Phantom.Common.Data;

namespace Phantom.Controller.Database.Converters;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
sealed class RamAllocationUnitsConverter() : ValueConverter<RamAllocationUnits, ushort>(
	static units => units.RawValue,
	static value => new RamAllocationUnits(value)
);
