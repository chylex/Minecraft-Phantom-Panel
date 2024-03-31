using System.Diagnostics.CodeAnalysis;
using MemoryPack;

namespace Phantom.Common.Data;

[MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial class Result<TValue, TError> {
	[MemoryPackOrder(0)]
	[MemoryPackInclude]
	private readonly bool hasValue;
	
	[MemoryPackOrder(1)]
	[MemoryPackInclude]
	private readonly TValue? value;
	
	[MemoryPackOrder(2)]
	[MemoryPackInclude]
	private readonly TError? error;

	[MemoryPackIgnore]
	public TValue Value => hasValue ? value! : throw new InvalidOperationException("Attempted to get value from an error result.");
	
	[MemoryPackIgnore]
	public TError Error => !hasValue ? error! : throw new InvalidOperationException("Attempted to get error from a success result.");
	
	private Result(bool hasValue, TValue? value, TError? error) {
		this.hasValue = hasValue;
		this.value = value;
		this.error = error;
	}

	public static implicit operator Result<TValue, TError>(TValue value) {
		return new Result<TValue, TError>(hasValue: true, value, default);
	}
	
	public static implicit operator Result<TValue, TError>(TError error) {
		return new Result<TValue, TError>(hasValue: false, default, error);
	}

	public static implicit operator bool(Result<TValue, TError> result) {
		return result.hasValue;
	}
}

[MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial class Result<TError> {
	[MemoryPackOrder(0)]
	[MemoryPackInclude]
	private readonly bool hasValue;
	
	[MemoryPackOrder(1)]
	[MemoryPackInclude]
	private readonly TError? error;

	[MemoryPackIgnore]
	public TError Error => !hasValue ? error! : throw new InvalidOperationException("Attempted to get error from a success result.");

	private Result(bool hasValue, TError? error) {
		this.hasValue = hasValue;
		this.error = error;
	}

	public bool TryGetError([MaybeNullWhen(false)] out TError error) {
		if (hasValue) {
			error = default;
			return false;
		}
		else {
			error = this.error!;
			return true;
		}
	}

	public static implicit operator Result<TError>([SuppressMessage("ReSharper", "UnusedParameter.Global")] Result.OkType _) {
		return new Result<TError>(hasValue: true, default);
	}

	public static implicit operator Result<TError>(TError error) {
		return new Result<TError>(hasValue: false, error);
	}

	public static implicit operator bool(Result<TError> result) {
		return result.hasValue;
	}
}

public static class Result {
	public static OkType Ok { get; }  = new ();

	public readonly record struct OkType;
}
