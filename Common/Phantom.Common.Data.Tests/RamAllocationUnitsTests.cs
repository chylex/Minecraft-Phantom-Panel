using NUnit.Framework;

namespace Phantom.Common.Data.Tests;

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
		
		[TestCase(-256)]
		[TestCase(int.MinValue)]
		public void LessThan256MegabytesThrows(int value) {
			Assert.That(CallFromMegabytes(value), Throws.Exception.TypeOf<ArgumentOutOfRangeException>().With.Message.StartsWith("Must be at least 0 MB."));
		}
		
		[TestCase(16777216)]
		[TestCase(int.MaxValue - 255)]
		public void MoreThan16TerabytesThrows(int value) {
			Assert.That(CallFromMegabytes(value), Throws.Exception.TypeOf<ArgumentOutOfRangeException>().With.Message.StartsWith("Must be at most " + (256 * 65535) + " MB."));
		}
		
		[TestCase(0)]
		[TestCase(256)]
		[TestCase(512)]
		[TestCase(1024)]
		[TestCase(65536)]
		[TestCase(16777216 - 256)]
		public void ValidValueReturnsSameValueInMegabytes(int value) {
			Assert.That(RamAllocationUnits.FromMegabytes(value).InMegabytes, Is.EqualTo(value));
		}
	}
	
	public sealed class FromString {
		private Action CallFromString(string definition) {
			return () => RamAllocationUnits.FromString(definition);
		}
		
		[Test]
		public void EmptyThrows() {
			Assert.That(CallFromString(""), Throws.Exception.TypeOf<ArgumentOutOfRangeException>().With.Message.StartsWith("Must not be empty."));
		}
		
		[Test]
		public void MissingUnitThrows() {
			Assert.That(CallFromString("256"), Throws.Exception.TypeOf<ArgumentOutOfRangeException>().With.Message.StartsWith("Must end with "));
		}
		
		[Test]
		public void InvalidUnitThrows() {
			Assert.That(CallFromString("256R"), Throws.Exception.TypeOf<ArgumentOutOfRangeException>().With.Message.StartsWith("Must end with "));
		}
		
		[Test]
		public void UnparseableValueThrows() {
			Assert.That(CallFromString("123A5M"), Throws.Exception.TypeOf<ArgumentOutOfRangeException>().With.Message.StartsWith("Must begin with a number."));
		}
		
		[TestCase("0m", arg2: 0)]
		[TestCase("256m", arg2: 256)]
		[TestCase("256M", arg2: 256)]
		[TestCase("512M", arg2: 512)]
		[TestCase("65536M", arg2: 65536)]
		[TestCase("16776960M", 16777216 - 256)]
		public void ValidDefinitionInMegabytesIsParsedCorrectly(string definition, int megabytes) {
			Assert.That(RamAllocationUnits.FromString(definition).InMegabytes, Is.EqualTo(megabytes));
		}
		
		[TestCase("0g", arg2: 0)]
		[TestCase("1g", arg2: 1024)]
		[TestCase("1G", arg2: 1024)]
		[TestCase("8G", arg2: 8192)]
		[TestCase("64G", arg2: 65536)]
		[TestCase("16383G", arg2: 16776192)]
		public void ValidDefinitionInGigabytesIsParsedCorrectly(string definition, int megabytes) {
			Assert.That(RamAllocationUnits.FromString(definition).InMegabytes, Is.EqualTo(megabytes));
		}
	}
}
