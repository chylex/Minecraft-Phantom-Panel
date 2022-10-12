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
		private bool IsMissing => kind == ValueKind.Missing;

		private Value(T? value, ValueKind kind, string variableName, string errorMessage) {
			this.value = value;
			this.kind = kind;
			this.variableName = variableName;
			this.errorMessage = errorMessage;
		}
		
		public T Require                                    => HasValue ? value! : throw new Exception(errorMessage + ": " + variableName);
		public T WithDefault(T defaultValue)                => IsMissing ? defaultValue : Require;
		public T WithDefaultGetter(Func<T> getDefaultValue) => IsMissing ? getDefaultValue() : Require;

		internal Value<TResult> Map<TResult>(Func<T, TResult> mapper, Func<Exception, string> mapperThrowingErrorMessage) where TResult : notnull {
			if (kind is ValueKind.Missing or ValueKind.HasError) {
				return new Value<TResult>(default, kind, variableName, errorMessage);
			}

			try {
				return Value<TResult>.Of(mapper(value!), variableName);
			} catch (Exception e) {
				return Value<TResult>.Error(variableName, mapperThrowingErrorMessage(e));
			}
		}

		internal Value<TResult> Map<TResult>(Func<T, TResult> mapper, string mapperThrowingErrorMessage) where TResult : notnull {
			return Map(mapper, _ => mapperThrowingErrorMessage);
		}

		public Value<TResult> MapParse<TResult>(Func<T, TResult> mapper) where TResult : notnull {
			return Map(mapper, static e => "Environment variable has invalid format: " + e.Message);
		}

		public Value<T> Validate(Predicate<T> predicate, string errorMessage) {
			return Map(value => predicate(value) ? value : throw new Exception(), errorMessage);
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

	public static Value<int> GetInteger(string variableName) {
		return GetString(variableName).Map(int.Parse, "Environment variable must be a 32-bit integer");
	}

	public static Value<int> GetInteger(string variableName, int min, int max) {
		return GetInteger(variableName).Map(value => value >= min && value <= max ? value : throw new Exception(), "Environment variable must be between " + min + " and " + max);
	}

	public static Value<ushort> GetPortNumber(string variableName) {
		return GetString(variableName).Map(ushort.Parse, "Environment variable must be a port number");
	}
}
