namespace Phantom.Utils.Monads;

public abstract record Either<TLeft, TRight> {
	public abstract bool IsLeft { get; }
	public abstract bool IsRight { get; }
	
	public abstract TLeft RequireLeft { get; }
	public abstract TRight RequireRight { get; }
	
	public abstract Either<TNewLeft, TRight> MapLeft<TNewLeft>(Func<TLeft, TNewLeft> func);
	public abstract Either<TLeft, TNewRight> MapRight<TNewRight>(Func<TRight, TNewRight> func);
	
	public static implicit operator Either<TLeft, TRight>(Left<TLeft> value) => new Left<TLeft, TRight>(value.Value);
	public static implicit operator Either<TLeft, TRight>(Right<TRight> value) => new Right<TLeft, TRight>(value.Value);
}

public static class Either {
	public static Left<TValue> Left<TValue>(TValue value) {
		return new Left<TValue>(value);
	}
	
	public static Right<TValue> Right<TValue>(TValue value) {
		return new Right<TValue>(value);
	}
}
