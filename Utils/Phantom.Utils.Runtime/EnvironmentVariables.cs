namespace Phantom.Utils.Runtime;

public static class EnvironmentVariables {
	private enum ValueKind {
		Missing,
		HasValue,
		HasError
	}

	public readonly struct Value<T> where T : notnull {
		private readonly T? value;
		private readonly ValueKind kind;
		private readonly string variableName;
		private readonly string errorMessage;

		internal static Value<T> Missing(string variableName, string errorMessage) {
			return new Value<T>(default, ValueKind.Missing, variableName, errorMessage);
		}

		internal static Value<T> Of(T value, string variableName) {
			return new Value<T>(value, ValueKind.HasValue, variableName, string.Empty);
		}

		private static Value<T> Error(string variableName, string errorMessage) {
			return new Value<T>(default, ValueKind.HasError, variableName, errorMessage);
		}

		private Value(T? value, ValueKind kind, string variableName, string errorMessage) {
			this.value = value;
			this.kind = kind;
			this.variableName = variableName;
			this.errorMessage = errorMessage;
		}

		public T OrThrow => kind == ValueKind.HasValue ? value! : throw new Exception(errorMessage + ": " + variableName);

		public T OrDefault(T defaultValue) {
			return kind == ValueKind.HasValue ? value! : defaultValue;
		}

		internal Value<TResult> Map<TResult>(Func<T, TResult> mapper, string mapperThrowingErrorMessage) where TResult : notnull {
			if (kind is ValueKind.Missing or ValueKind.HasError) {
				return new Value<TResult>(default, kind, variableName, errorMessage);
			}

			try {
				return Value<TResult>.Of(mapper(value!), variableName);
			} catch (Exception) {
				return Value<TResult>.Error(variableName, mapperThrowingErrorMessage);
			}
		}
	}

	public static Value<string> GetString(string variableName) {
		var value = Environment.GetEnvironmentVariable(variableName);
		return value == null ? Value<string>.Missing(variableName, "Missing environment variable") : Value<string>.Of(value, variableName);
	}

	public static Value<ushort> GetPortNumber(string variableName) {
		return GetString(variableName).Map(ushort.Parse, "Environment variable must be a port number");
	}
}
