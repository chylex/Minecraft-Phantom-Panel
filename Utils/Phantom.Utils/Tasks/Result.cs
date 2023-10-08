namespace Phantom.Utils.Tasks;

public abstract record Result<TValue, TError> {
	private Result() {}
	
	public sealed record Ok(TValue Value) : Result<TValue, TError>;
	
	public sealed record Fail(TError Error) : Result<TValue, TError>;
}

public abstract record Result<TError> {
	private Result() {}

	public sealed record Ok : Result<TError> {
		internal static Ok Instance { get; } = new ();
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
