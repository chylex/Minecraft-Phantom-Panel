using System.Runtime.CompilerServices;

namespace Phantom.Utils.Collections;

public sealed class ReferenceEqualityComparer<T> : IEqualityComparer<T> {
	public static readonly ReferenceEqualityComparer<T> Instance = new ();

	public bool Equals(T? x, T? y) {
		return ReferenceEquals(x, y);
	}

	public int GetHashCode(T obj) {
		return RuntimeHelpers.GetHashCode(obj);
	}
}
