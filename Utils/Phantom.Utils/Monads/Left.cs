namespace Phantom.Utils.Monads;

public sealed record Left<TLeft, TRight>(TLeft Value) : Either<TLeft, TRight> {
	public override bool IsLeft => true;
	public override bool IsRight => false;
	
	public override TLeft RequireLeft => Value;
	public override TRight RequireRight => throw new InvalidOperationException("Either<" + typeof(TLeft).Name + ", " + typeof(TRight).Name + "> has a left value, but right value was requested.");
	
	public override Either<TNewLeft, TRight> MapLeft<TNewLeft>(Func<TLeft, TNewLeft> func) {
		return new Left<TNewLeft, TRight>(func(Value));
	}
	
	public override Either<TLeft, TNewRight> MapRight<TNewRight>(Func<TRight, TNewRight> func) {
		return new Left<TLeft, TNewRight>(Value);
	}
}

public sealed record Left<TValue>(TValue Value);
