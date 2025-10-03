using System.Buffers;
using System.Collections;

namespace Phantom.Utils.Collections;

public ref struct SpanIndexEnumerator<T>(ReadOnlySpan<T> span, SearchValues<T> searchValues) : IEnumerator<int> where T : IEquatable<T> {
	private readonly ReadOnlySpan<T> span = span;
	
	public int Current { get; private set; } = -1;
	
	readonly object IEnumerator.Current => Current;
	
	public readonly SpanIndexEnumerator<T> GetEnumerator() => this;
	
	public bool MoveNext() {
		int startIndex = Current + 1;
		int relativeIndex = span[startIndex..].IndexOfAny(searchValues);
		if (relativeIndex == -1) {
			return false;
		}
		
		Current = startIndex + relativeIndex;
		return true;
	}
	
	public void Reset() {
		Current = -1;
	}
	
	public void Dispose() {}
}

public static class SpanIndexEnumeratorExtensions {
	public static SpanIndexEnumerator<T> IndicesOf<T>(this ReadOnlySpan<T> span, SearchValues<T> searchValues) where T : IEquatable<T> {
		return new SpanIndexEnumerator<T>(span, searchValues);
	}
}
