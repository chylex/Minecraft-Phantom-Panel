using NUnit.Framework;

namespace Phantom.Utils.Runtime.Tests;

[TestFixture]
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public class EnvironmentVariablesTests {
	private const string VariableNameMissing = "PhantomTestMissing";
	private const string VariableNameExistingPrefix = "PhantomTest_";

	private readonly HashSet<string> createdVariables = new ();

	private static void Discard<T>(T value) {
		var _ = value;
	}

	private string CreateVariable(string value) {
		string name = VariableNameExistingPrefix + Guid.NewGuid();
		Environment.SetEnvironmentVariable(name, value, EnvironmentVariableTarget.Process);
		createdVariables.Add(name);
		return name;
	}

	[TearDown]
	public void RemoveCreatedVariables() {
		foreach (var variable in createdVariables) {
			Environment.SetEnvironmentVariable(variable, null, EnvironmentVariableTarget.Process);
		}
	}

	public abstract class Base<T> : EnvironmentVariablesTests where T : notnull {
		protected abstract T ExampleValue { get; }
		protected abstract string ExampleValueString { get; }

		protected abstract EnvironmentVariables.Value<T> GetValue(string variableName);

		protected Action CallGetValueAndRequire(string variableName) {
			return () => Discard(GetValue(variableName).Require);
		}

		protected Action CallGetValueWithDefault(string variableName) {
			return () => Discard(GetValue(variableName).WithDefault(ExampleValue));
		}

		[Test]
		public void RequireWithMissingThrows() {
			Assert.That(CallGetValueAndRequire(VariableNameMissing), Throws.Exception.Message.EqualTo("Missing environment variable: " + VariableNameMissing));
		}

		[Test]
		public void MissingWithDefaultReturnsDefaultValue() {
			Assert.That(GetValue(VariableNameMissing).WithDefault(ExampleValue), Is.EqualTo(ExampleValue));
		}

		[Test]
		public void MissingWithDefaultGetterReturnsDefaultValue() {
			Assert.That(GetValue(VariableNameMissing).WithDefaultGetter(() => ExampleValue), Is.EqualTo(ExampleValue));
		}
		
		[Test]
		public void RequireWithExistingReturnsActualValue() {
			Assume.That(ExampleValue, Is.Not.EqualTo(default));
			Assert.That(GetValue(CreateVariable(ExampleValueString)).Require, Is.EqualTo(ExampleValue));
		}

		[Test]
		public void ExistingWithDefaultReturnsActualValue() {
			Assume.That(ExampleValue, Is.Not.EqualTo(default));
			Assert.That(GetValue(CreateVariable(ExampleValueString)).WithDefault(default!), Is.EqualTo(ExampleValue));
		}

		[Test]
		public void ExistingWithDefaultGetterReturnsActualValue() {
			Assume.That(ExampleValue, Is.Not.EqualTo(default));
			Assert.That(GetValue(CreateVariable(ExampleValueString)).WithDefaultGetter(static () => default!), Is.EqualTo(ExampleValue));
		}

		[Test]
		public void ExistingWithDefaultGetterDoesNotCallDefaultGetter() {
			GetValue(CreateVariable(ExampleValueString)).WithDefaultGetter(static () => {
				Assert.Fail();
				return default!;
			});
		}
	}

	public sealed class GetString : Base<string> {
		protected override string ExampleValue => "abc";
		protected override string ExampleValueString => ExampleValue;

		protected override EnvironmentVariables.Value<string> GetValue(string variableName) {
			return EnvironmentVariables.GetString(variableName);
		}
	}

	public sealed class GetInteger : Base<int> {
		protected override int ExampleValue => 2_147_483_647;
		protected override string ExampleValueString => "2147483647";

		protected override EnvironmentVariables.Value<int> GetValue(string variableName) {
			return EnvironmentVariables.GetInteger(variableName);
		}

		[Test]
		public void UnparseableOrThrowThrows() {
			Assert.That(CallGetValueAndRequire(CreateVariable("2147483648")), Throws.Exception.Message.StartsWith("Environment variable must be a 32-bit integer: " + VariableNameExistingPrefix));
		}

		[Test]
		public void UnparseableWithDefaultThrows() {
			Assert.That(CallGetValueWithDefault(CreateVariable("2147483648")), Throws.Exception.Message.StartsWith("Environment variable must be a 32-bit integer: " + VariableNameExistingPrefix));
		}
	}

	public sealed class GetIntegerInRange : Base<int> {
		protected override int ExampleValue => 5000;
		protected override string ExampleValueString => "5000";

		protected override EnvironmentVariables.Value<int> GetValue(string variableName) {
			return EnvironmentVariables.GetInteger(variableName, min: 1000, max: 6000);
		}

		[TestCase("1000", 1000)]
		[TestCase("6000", 6000)]
		public void JustInRangeOrThrowReturnsActualValue(string inputValue, int returnedValue) {
			Assert.That(GetValue(CreateVariable(inputValue)).Require, Is.EqualTo(returnedValue));
		}

		[TestCase("999")]
		[TestCase("6001")]
		public void OutsideRangeOrThrowThrows(string value) {
			Assert.That(CallGetValueAndRequire(CreateVariable(value)), Throws.Exception.Message.StartsWith("Environment variable must be between 1000 and 6000: " + VariableNameExistingPrefix));
		}

		[TestCase("999")]
		[TestCase("6001")]
		public void OutsideRangeWithDefaultThrows(string value) {
			Assert.That(CallGetValueWithDefault(CreateVariable(value)), Throws.Exception.Message.StartsWith("Environment variable must be between 1000 and 6000: " + VariableNameExistingPrefix));
		}
	}

	public sealed class GetPortNumber : Base<ushort> {
		protected override ushort ExampleValue => 12345;
		protected override string ExampleValueString => "12345";

		protected override EnvironmentVariables.Value<ushort> GetValue(string variableName) {
			return EnvironmentVariables.GetPortNumber(variableName);
		}

		[Test]
		public void UnparseableOrThrowThrows() {
			Assert.That(CallGetValueAndRequire(CreateVariable("654321")), Throws.Exception.Message.StartsWith("Environment variable must be a port number: " + VariableNameExistingPrefix));
		}

		[Test]
		public void UnparseableWithDefaultThrows() {
			Assert.That(CallGetValueWithDefault(CreateVariable("654321")), Throws.Exception.Message.StartsWith("Environment variable must be a port number: " + VariableNameExistingPrefix));
		}
	}
}
