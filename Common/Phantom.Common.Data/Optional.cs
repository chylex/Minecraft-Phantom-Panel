using MemoryPack;

namespace Phantom.Common.Data;

[MemoryPackable]
public readonly partial record struct Optional<T>(T? Value) {
	public static implicit operator Optional<T>(T? value) {
		return new Optional<T>(value);
	}
}
