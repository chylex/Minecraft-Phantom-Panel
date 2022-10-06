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

		protected Action CallGetValueOrThrow(string variableName) {
			return () => Discard(GetValue(variableName).OrThrow);
		}

		[Test]
		public void MissingOrThrowThrows() {
			Assert.That(CallGetValueOrThrow(VariableNameMissing), Throws.Exception.Message.EqualTo("Missing environment variable: " + VariableNameMissing));
		}

		[Test]
		public void MissingOrDefaultReturnsDefaultValue() {
			Assert.That(GetValue(VariableNameMissing).OrDefault(ExampleValue), Is.EqualTo(ExampleValue));
		}

		[Test]
		public void MissingOrGetDefaultReturnsDefaultValue() {
			Assert.That(GetValue(VariableNameMissing).OrGetDefault(() => ExampleValue), Is.EqualTo(ExampleValue));
		}
		
		[Test]
		public void ExistingOrThrowReturnsActualValue() {
			Assume.That(ExampleValue, Is.Not.EqualTo(default));
			Assert.That(GetValue(CreateVariable(ExampleValueString)).OrThrow, Is.EqualTo(ExampleValue));
		}

		[Test]
		public void ExistingOrDefaultReturnsActualValue() {
			Assume.That(ExampleValue, Is.Not.EqualTo(default));
			Assert.That(GetValue(CreateVariable(ExampleValueString)).OrDefault(default!), Is.EqualTo(ExampleValue));
		}

		[Test]
		public void ExistingOrGetDefaultReturnsActualValue() {
			Assume.That(ExampleValue, Is.Not.EqualTo(default));
			Assert.That(GetValue(CreateVariable(ExampleValueString)).OrGetDefault(static () => default!), Is.EqualTo(ExampleValue));
		}

		[Test]
		public void ExistingOrGetDefaultDoesNotCallDefaultGetter() {
			GetValue(CreateVariable(ExampleValueString)).OrGetDefault(static () => {
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
			Assert.That(CallGetValueOrThrow(CreateVariable("2147483648")), Throws.Exception.Message.StartsWith("Environment variable must be a 32-bit integer: " + VariableNameExistingPrefix));
		}

		[Test]
		public void UnparseableOrDefaultReturnsDefaultValue() {
			Assert.That(GetValue(CreateVariable("2147483648")).OrDefault(ExampleValue), Is.EqualTo(ExampleValue));
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
			Assert.That(CallGetValueOrThrow(CreateVariable("654321")), Throws.Exception.Message.StartsWith("Environment variable must be a port number: " + VariableNameExistingPrefix));
		}

		[Test]
		public void UnparseableOrDefaultReturnsDefaultValue() {
			Assert.That(GetValue(CreateVariable("654321")).OrDefault(ExampleValue), Is.EqualTo(ExampleValue));
		}
	}
}
