namespace Phantom.Utils.Tasks;

public abstract record Result<TValue, TError> {
	private Result() {}

	public abstract TValue Value { get; init; }
	public abstract TError Error { get; init; }

	public static implicit operator Result<TValue, TError>(TValue value) {
		return new Ok(value);
	}
	
	public static implicit operator Result<TValue, TError>(TError error) {
		return new Fail(error);
	}

	public static implicit operator bool(Result<TValue, TError> result) {
		return result is Ok;
	}
	
	public sealed record Ok(TValue Value) : Result<TValue, TError> {
		public override TError Error {
			get => throw new InvalidOperationException("Attempted to get error from Ok result.");
			init {}
		}
	}

	public sealed record Fail(TError Error) : Result<TValue, TError> {
		public override TValue Value {
			get => throw new InvalidOperationException("Attempted to get value from Fail result.");
			init {}
		}
	}
}

public abstract record Result<TError> {
	private Result() {}
	
	public abstract TError Error { get; init; }

	public static implicit operator Result<TError>(TError error) {
		return new Fail(error);
	}
	
	public static implicit operator bool(Result<TError> result) {
		return result is Ok;
	}
	
	public sealed record Ok : Result<TError> {
		internal static Ok Instance { get; } = new ();
		
		public override TError Error {
			get => throw new InvalidOperationException("Attempted to get error from Ok result.");
			init {}
		}
	}
	
	public sealed record Fail(TError Error) : Result<TError>;
}

public static class Result {
	public static Result<TError> Ok<TError>() {
		return Result<TError>.Ok.Instance;
	}
	
	public static Result<TError> Fail<TError>(TError error) {
		return new Result<TError>.Fail(error);
	}
	
	public static Result<TValue, TError> Ok<TValue, TError>(TValue value) {
		return new Result<TValue, TError>.Ok(value);
	}
	
	public static Result<TValue, TError> Fail<TValue, TError>(TError error) {
		return new Result<TValue, TError>.Fail(error);
	}
}
