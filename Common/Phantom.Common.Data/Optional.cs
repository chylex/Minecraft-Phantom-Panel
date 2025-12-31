using MemoryPack;

namespace Phantom.Common.Data;

[MemoryPackable]
public readonly partial struct Optional<T> {
	[MemoryPackOrder(0)]
	public bool HasValue { get; }
	
	[MemoryPackOrder(1)]
	public T Value {
		get {
			if (HasValue) {
				return field!;
			}
			else {
				throw new InvalidOperationException();
			}
		}
	}
	
	[MemoryPackIgnore]
	public T? ValueOrDefault => HasValue ? Value : default;
	
	public Optional() : this(hasValue: false, value: default) {}
	public Optional(T value) : this(hasValue: true, value) {}
	
	[MemoryPackConstructor]
	private Optional(bool hasValue, T? value) {
		this.HasValue = hasValue;
		this.Value = value;
	}
	
	public Optional<R> Map<R>(Func<T, R> func) {
		return HasValue ? new Optional<R>(func(Value)) : new Optional<R>();
	}
	
	public T Or(T fallbackValue) {
		return HasValue ? Value : fallbackValue;
	}
	
	public Optional<T> Or(Optional<T> fallbackOptional) {
		return HasValue ? Value : fallbackOptional;
	}
	
	public static implicit operator Optional<T>(T value) {
		return new Optional<T>(value);
	}
}
