namespace Phantom.Utils.Result;

public sealed record Ok<T>(T Value) : Result;
