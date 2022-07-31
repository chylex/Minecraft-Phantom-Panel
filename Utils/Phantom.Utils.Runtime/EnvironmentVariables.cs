namespace Phantom.Utils.Runtime;

public static class EnvironmentVariables {
	private enum ValueKind {
		Missing,
		HasValue,
		HasError
	}

	public readonly struct Value<T> where T : notnull {
		internal static Value<T> Missing(string variableName, string errorMessage) {
			return new Value<T>(default, ValueKind.Missing, variableName, errorMessage);
		}

		internal static Value<T> Of(T value, string variableName) {
			return new Value<T>(value, ValueKind.HasValue, variableName, string.Empty);
		}

		private static Value<T> Error(string variableName, string errorMessage) {
			return new Value<T>(default, ValueKind.HasError, variableName, errorMessage);
		}
		
		private readonly T? value;
		private readonly ValueKind kind;
		private readonly string variableName;
		private readonly string errorMessage;

		private bool HasValue => kind == ValueKind.HasValue;

		private Value(T? value, ValueKind kind, string variableName, string errorMessage) {
			this.value = value;
			this.kind = kind;
			this.variableName = variableName;
			this.errorMessage = errorMessage;
		}

		public T OrThrow                   => HasValue ? value! : throw new Exception(errorMessage + ": " + variableName);
		public T OrDefault(T defaultValue) => HasValue ? value! : defaultValue;

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

	public static Value<(string?, string?)> GetEitherString(string leftVariableName, string rightVariableName) {
		string? leftValue = Environment.GetEnvironmentVariable(leftVariableName);
		string? rightValue = Environment.GetEnvironmentVariable(rightVariableName);

		if (leftValue == null && rightValue == null) {
			return Value<(string?, string?)>.Missing(leftVariableName + " / " + rightVariableName, "Missing environment variable");
		}

		if (leftValue != null && rightValue != null) {
			return Value<(string?, string?)>.Missing(leftVariableName + " / " + rightVariableName, "Only one of these environment variables must be used, but not both");
		}
		
		return Value<(string?, string?)>.Of((leftValue, rightValue), leftValue == null ? rightVariableName : leftVariableName);
	}

	public static Value<ushort> GetPortNumber(string variableName) {
		return GetString(variableName).Map(ushort.Parse, "Environment variable must be a port number");
	}
}
