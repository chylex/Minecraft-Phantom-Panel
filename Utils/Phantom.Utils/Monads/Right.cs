namespace Phantom.Utils.Monads;

public sealed record Right<TLeft, TRight>(TRight Value) : Either<TLeft, TRight> {
	public override bool IsLeft => false;
	public override bool IsRight => true;
	
	public override TLeft RequireLeft => throw new InvalidOperationException("Either<" + typeof(TLeft).Name + ", " + typeof(TRight).Name + "> has a right value, but left value was requested.");
	public override TRight RequireRight => Value;
	
	public override Either<TNewLeft, TRight> MapLeft<TNewLeft>(Func<TLeft, TNewLeft> func) {
		return new Right<TNewLeft, TRight>(Value);
	}
	
	public override Either<TLeft, TNewRight> MapRight<TNewRight>(Func<TRight, TNewRight> func) {
		return new Right<TLeft, TNewRight>(func(Value));
	}
}

public sealed record Right<TValue>(TValue Value);
