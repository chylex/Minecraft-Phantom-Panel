using NUnit.Framework;
using Phantom.Common.Data;

namespace Tests.Phantom.Common.Data;

[TestFixture]
public sealed class RamAllocationUnitsTests {
	public sealed class FromMegabytes {
		private Action CallFromMegabytes(int value) {
			return () => RamAllocationUnits.FromMegabytes(value);
		}

		[TestCase(1)]
		[TestCase(-1)]
		[TestCase(255)]
		[TestCase(-255)]
		[TestCase(int.MaxValue)]
		[TestCase(int.MinValue + 1)]
		public void NotMultipleOf256MegabytesThrows(int value) {
			Assert.That(CallFromMegabytes(value), Throws.Exception.TypeOf<ArgumentOutOfRangeException>().With.Message.StartsWith("Must be a multiple of 256 MB."));
		}
		
		[TestCase(0)]
		[TestCase(-256)]
		[TestCase(int.MinValue)]
		public void LessThan256MegabytesThrows(int value) {
			Assert.That(CallFromMegabytes(value), Throws.Exception.TypeOf<ArgumentOutOfRangeException>().With.Message.StartsWith("Must be at least 256 MB."));
		}

		[TestCase(16777216 + 256)]
		[TestCase(int.MaxValue - 255)]
		public void MoreThan16TerabytesThrows(int value) {
			Assert.That(CallFromMegabytes(value), Throws.Exception.TypeOf<ArgumentOutOfRangeException>().With.Message.StartsWith("Must be at most " + (256 * 65536) + " MB."));
		}

		[TestCase(256)]
		[TestCase(512)]
		[TestCase(1024)]
		[TestCase(65536)]
		[TestCase(16777216)]
		public void ValidValueReturnsSameValueInMegabytes(int value) {
			Assert.That(RamAllocationUnits.FromMegabytes(value).InMegabytes, Is.EqualTo(value));
		}
	}
}
