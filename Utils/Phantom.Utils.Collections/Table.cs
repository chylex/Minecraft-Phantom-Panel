using System.Collections;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Phantom.Utils.Collections;

public sealed class Table<TRow, TKey> : IReadOnlyList<TRow>, IReadOnlyDictionary<TKey, TRow> where TRow : notnull where TKey : notnull {
	private readonly List<TRow> rowList = new();
	private readonly Dictionary<TKey, TRow> rowDictionary = new ();

	public TRow this[int index] => rowList[index];
	public TRow this[TKey key] => rowDictionary[key];

	public IEnumerable<TKey> Keys => rowDictionary.Keys;
	public IEnumerable<TRow> Values => rowDictionary.Values;
	
	public bool IsEmpty => rowList.Count == 0;
	public int Count => rowList.Count;

	public bool ContainsKey(TKey key) {
		return rowDictionary.ContainsKey(key);
	}

	public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TRow value) {
		return rowDictionary.TryGetValue(key, out value);
	}

	public void UpdateFrom<TSource>(ImmutableArray<TSource> sourceItems, Func<TSource, TKey> getKey, Func<TSource, TRow> createRow, Action<TSource, TRow> updateRow) {
		HashSet<TKey> removedKeys = rowDictionary.Keys.ToHashSet();

		foreach (var sourceItem in sourceItems) {
			var key = getKey(sourceItem);
			if (rowDictionary.TryGetValue(key, out var row)) {
				updateRow(sourceItem, row);
				removedKeys.Remove(key);
			}
			else {
				row = createRow(sourceItem);
				rowList.Add(row);
				rowDictionary[key] = row;
			}
		}

		foreach (var key in removedKeys) {
			var row = rowDictionary[key];
			rowList.Remove(row);
			rowDictionary.Remove(key);
		}
	}

	public IEnumerator<TRow> GetEnumerator() {
		return rowList.GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator() {
		return GetEnumerator();
	}

	IEnumerator<KeyValuePair<TKey, TRow>> IEnumerable<KeyValuePair<TKey, TRow>>.GetEnumerator() {
		return rowDictionary.GetEnumerator();
	}
}
