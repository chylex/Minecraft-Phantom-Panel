using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Phantom.Utils.Collections;

public static class EnumerableExtensions {
	[SuppressMessage("ReSharper", "LoopCanBeConvertedToQuery")]
	public static IEnumerable<TSource> WhereNotNull<TSource>(this IEnumerable<TSource?> items) {
		foreach (var item in items) {
			if (item is not null) {
				yield return item;
			}
		}
	}
	
	public static async Task<ImmutableArray<TSource>> ToImmutableArrayAsync<TSource>(this IAsyncEnumerable<TSource> source, CancellationToken cancellationToken = default) {
		var builder = ImmutableArray.CreateBuilder<TSource>();
		
		await foreach (var element in source.WithCancellation(cancellationToken)) {
			builder.Add(element);
		}

		return builder.ToImmutable();
	}
	
	public static async Task<ImmutableHashSet<TSource>> ToImmutableSetAsync<TSource>(this IAsyncEnumerable<TSource> source, CancellationToken cancellationToken = default) {
		var builder = ImmutableHashSet.CreateBuilder<TSource>();
		
		await foreach (var element in source.WithCancellation(cancellationToken)) {
			builder.Add(element);
		}

		return builder.ToImmutable();
	}
	
	public static async Task<ImmutableDictionary<TKey, TValue>> ToImmutableDictionaryAsync<TSource, TKey, TValue>(this IAsyncEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TValue> valueSelector, CancellationToken cancellationToken = default) where TKey : notnull {
		var builder = ImmutableDictionary.CreateBuilder<TKey, TValue>();
		
		await foreach (var element in source.WithCancellation(cancellationToken)) {
			builder.Add(keySelector(element), valueSelector(element));
		}

		return builder.ToImmutable();
	}
}
