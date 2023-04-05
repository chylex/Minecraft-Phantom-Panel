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
}
