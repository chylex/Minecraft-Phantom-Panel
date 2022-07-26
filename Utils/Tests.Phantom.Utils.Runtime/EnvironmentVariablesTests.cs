using NUnit.Framework;
using Phantom.Utils.Runtime;

namespace Tests.Phantom.Utils.Runtime;

[TestFixture]
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public class EnvironmentVariablesTests {
	private const string VariableNameMissing = "PhantomTestMissing";
	private const string VariableNameExistingPrefix = "PhantomTest_";

	private readonly HashSet<string> createdVariables = new ();
	
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

	public abstract class Get<T> : EnvironmentVariablesTests where T : notnull {
		protected abstract T ExampleValue { get; }
		protected abstract EnvironmentVariables.Value<T> GetValue(string variableName);

		[Test]
		public void MissingOrThrowThrows() {
			Assert.That(() => {
				var _ = GetValue(VariableNameMissing).OrThrow;
			}, Throws.Exception.Message.EqualTo("Missing environment variable: " + VariableNameMissing));
		}

		[Test]
		public void MissingOrDefaultReturnsDefault() {
			Assert.That(GetValue(VariableNameMissing).OrDefault(ExampleValue), Is.EqualTo(ExampleValue));
		}
	}

	public sealed class GetString : Get<string> {
		protected override string ExampleValue => "abc";

		protected override EnvironmentVariables.Value<string> GetValue(string variableName) {
			return EnvironmentVariables.GetString(variableName);
		}

		[Test]
		public void ExistingOrThrowReturnsActualValue() {
			Assert.That(GetValue(CreateVariable("abc")).OrThrow, Is.EqualTo("abc"));
		}

		[Test]
		public void ExistingOrDefaultReturnsActualValue() {
			Assert.That(GetValue(CreateVariable("abc")).OrDefault("def"), Is.EqualTo("abc"));
		}
	}

	public sealed class GetPortNumber : Get<ushort> {
		protected override ushort ExampleValue => 12345;

		protected override EnvironmentVariables.Value<ushort> GetValue(string variableName) {
			return EnvironmentVariables.GetPortNumber(variableName);
		}

		[Test]
		public void ExistingOrThrowReturnsActualValue() {
			Assert.That(GetValue(CreateVariable("12345")).OrThrow, Is.EqualTo(12345));
		}

		[Test]
		public void ExistingOrDefaultReturnsActualValue() {
			Assert.That(GetValue(CreateVariable("12345")).OrDefault(54321), Is.EqualTo(12345));
		}
	}
}
