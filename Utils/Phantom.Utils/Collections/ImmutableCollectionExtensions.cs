using System.Collections.Immutable;

namespace Phantom.Utils.Collections;

public static class ImmutableCollectionExtensions {
	extension(ImmutableDictionary) {
		public static ImmutableDictionary<TKey, TValue> From<TKey, TValue>(ReadOnlySpan<(TKey, TValue)> pairs) where TKey : notnull {
			var builder = ImmutableDictionary.CreateBuilder<TKey, TValue>();
			
			foreach ((TKey key, TValue value) in pairs) {
				builder.Add(key, value);
			}
			
			return builder.ToImmutable();
		}
	}
}
