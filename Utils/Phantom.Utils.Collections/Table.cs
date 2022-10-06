using System.Collections;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

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

	private void AddRowInternal(TKey key, TRow row) {
		rowList.Add(row);
		rowDictionary[key] = row;
	}

	private void UpdateRowInternal(TKey key, int rowIndex, TRow row) {
		rowList[rowIndex] = row;
		rowDictionary[key] = row;
	}

	public bool ContainsKey(TKey key) {
		return rowDictionary.ContainsKey(key);
	}

	public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TRow value) {
		return rowDictionary.TryGetValue(key, out value);
	}

	public bool TryUpdateRow(TKey key, Func<TRow, TRow> updateRow) {
		if (rowDictionary.TryGetValue(key, out var row)) {
			int rowIndex = rowList.FindIndex(r => ReferenceEquals(r, row));
			UpdateRowInternal(key, rowIndex, updateRow(row));
			return true;
		}
		else {
			return false;
		}
	}

	public void UpdateFrom<TSource>(ImmutableArray<TSource> sourceItems, Func<TSource, TKey> getKey, Func<TSource, TRow> createRow, Func<TSource, TRow, TRow> updateRow) {
		Dictionary<TRow, int> rowIndices = Enumerable.Range(0, rowList.Count).ToDictionary(i => rowList[i], static i => i, RowReferenceEqualityComparer.Instance);
		HashSet<TKey> removedKeys = rowDictionary.Keys.ToHashSet();

		foreach (var sourceItem in sourceItems) {
			var key = getKey(sourceItem);
			if (rowDictionary.TryGetValue(key, out var row)) {
				UpdateRowInternal(key, rowIndices[row], updateRow(sourceItem, row));
				removedKeys.Remove(key);
			}
			else {
				AddRowInternal(key, createRow(sourceItem));
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

	private sealed class RowReferenceEqualityComparer : IEqualityComparer<TRow> {
		public static readonly RowReferenceEqualityComparer Instance = new ();

		public bool Equals(TRow? x, TRow? y) {
			return ReferenceEquals(x, y);
		}

		public int GetHashCode(TRow obj) {
			return RuntimeHelpers.GetHashCode(obj);
		}
	}
}
