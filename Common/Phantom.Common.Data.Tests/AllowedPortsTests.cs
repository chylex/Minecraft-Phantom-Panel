using NUnit.Framework;

namespace Phantom.Common.Data.Tests;

[TestFixture]
public sealed class AllowedPortsTests {
	public sealed class FromString {
		private Action CallFromString(string definitions) {
			return () => AllowedPorts.FromString(definitions);
		}
		
		[Test]
		public void EmptyIsValid() {
			Assert.That(AllowedPorts.FromString(string.Empty).ToString(), Is.EqualTo(string.Empty));
		}
		
		[Test]
		public void ValidSinglePort() {
			Assert.That(AllowedPorts.FromString("12345").ToString(), Is.EqualTo("12345"));
		}
		
		[Test]
		public void ValidPortRange() {
			Assert.That(AllowedPorts.FromString("21000-34000").ToString(), Is.EqualTo("21000-34000"));
		}
		
		[Test]
		public void ValidPortRangeWithSamePort() {
			Assert.That(AllowedPorts.FromString("21000-21000").ToString(), Is.EqualTo("21000"));
		}
		
		[Test]
		public void ValidMultiplePorts() {
			Assert.That(AllowedPorts.FromString("12345,21000-34000").ToString(), Is.EqualTo("12345,21000-34000"));
		}
		
		[Test]
		public void SpacesAreTrimmed() {
			Assert.That(AllowedPorts.FromString(" 12345 ,  21000 - 34000  ").ToString(), Is.EqualTo("12345,21000-34000"));
		}
		
		[Test]
		public void InvalidSinglePort() {
			Assert.That(CallFromString("70000"), Throws.Exception.TypeOf<FormatException>().With.Message.EqualTo("Invalid port '70000'."));
		}
		
		[Test]
		public void InvalidFirstPortInRange() {
			Assert.That(CallFromString("abcde-34000"), Throws.Exception.TypeOf<FormatException>().With.Message.EqualTo("Invalid port 'abcde'."));
		}
		
		[Test]
		public void InvalidLastPortInRange() {
			Assert.That(CallFromString("21000-abcde"), Throws.Exception.TypeOf<FormatException>().With.Message.EqualTo("Invalid port 'abcde'."));
		}
		
		[Test]
		public void LastPortInRangeMustNotBeSmallerThanFirstPort() {
			Assert.That(CallFromString("21001-21000"), Throws.Exception.TypeOf<FormatException>().With.Message.EqualTo("Invalid port range '21001-21000'."));
		}
	}
}
