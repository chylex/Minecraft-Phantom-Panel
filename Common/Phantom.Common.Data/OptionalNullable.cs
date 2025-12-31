using MemoryPack;

namespace Phantom.Common.Data;

[MemoryPackable]
public readonly partial struct OptionalNullable<T> {
	[MemoryPackOrder(0)]
	public bool HasValue { get; }
	
	[MemoryPackOrder(1)]
	public T? Value {
		get {
			if (HasValue) {
				return field;
			}
			else {
				throw new InvalidOperationException();
			}
		}
	}
	
	public OptionalNullable() : this(hasValue: false, value: default) {}
	public OptionalNullable(T? value) : this(hasValue: true, value) {}
	
	[MemoryPackConstructor]
	private OptionalNullable(bool hasValue, T? value) {
		this.HasValue = hasValue;
		this.Value = value;
	}
	
	public T? Or(T? fallbackValue) {
		return HasValue ? Value : fallbackValue;
	}
	
	public OptionalNullable<T> Or(OptionalNullable<T> fallbackOptional) {
		return HasValue ? Value : fallbackOptional;
	}
	
	public static implicit operator OptionalNullable<T>(T? value) {
		return new OptionalNullable<T>(value);
	}
}
