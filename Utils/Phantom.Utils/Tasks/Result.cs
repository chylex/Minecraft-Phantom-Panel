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

	public static implicit operator Result<TError>(Result.OkType _) {
		return new Ok();
	}
	
	public static implicit operator bool(Result<TError> result) {
		return result is Ok;
	}
	
	public sealed record Ok : Result<TError> {
		public override TError Error {
			get => throw new InvalidOperationException("Attempted to get error from Ok result.");
			init {}
		}
	}
	
	public sealed record Fail(TError Error) : Result<TError>;
}

public static class Result {
	public static OkType Ok { get; }  = new ();

	public readonly record struct OkType;
}
