namespace Phantom.Utils.Result;

public sealed record Err<T>(T Error) : Result;
