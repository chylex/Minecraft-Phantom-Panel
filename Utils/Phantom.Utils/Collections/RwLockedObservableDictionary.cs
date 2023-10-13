using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Phantom.Utils.Collections;

public sealed class RwLockedObservableDictionary<TKey, TValue> where TKey : notnull {
	public event EventHandler? CollectionChanged;

	private readonly RwLockedDictionary<TKey, TValue> dict;

	public RwLockedObservableDictionary(LockRecursionPolicy recursionPolicy) {
		this.dict = new RwLockedDictionary<TKey, TValue>(recursionPolicy);
	}

	public RwLockedObservableDictionary(int capacity, LockRecursionPolicy recursionPolicy) {
		this.dict = new RwLockedDictionary<TKey, TValue>(capacity, recursionPolicy);
	}

	private void FireCollectionChanged() {
		CollectionChanged?.Invoke(this, EventArgs.Empty);
	}

	private bool FireCollectionChangedIf(bool result) {
		if (result) {
			FireCollectionChanged();
			return true;
		}
		else {
			return false;
		}
	}

	public TValue this[TKey key] {
		get => dict[key];
		set {
			dict[key] = value;
			FireCollectionChanged();
		}
	}

	public ImmutableArray<TValue> ValuesCopy => dict.ValuesCopy;

	public void ForEachValue(Action<TValue> action) {
		dict.ForEachValue(action);
	}

	public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value) {
		return dict.TryGetValue(key, out value);
	}
	
	public bool GetOrAdd(TKey key, Func<TKey, TValue> valueFactory, out TValue value) {
		return FireCollectionChangedIf(dict.GetOrAdd(key, valueFactory, out value));
	}

	public bool TryAdd(TKey key, TValue newValue) {
		return FireCollectionChangedIf(dict.TryAdd(key, newValue));
	}

	public bool AddOrReplace(TKey key, TValue newValue, [MaybeNullWhen(false)] out TValue oldValue) {
		return FireCollectionChangedIf(dict.AddOrReplace(key, newValue, out oldValue));
	}

	public bool AddOrReplaceIf(TKey key, TValue newValue, Predicate<TValue> replaceCondition) {
		return FireCollectionChangedIf(dict.AddOrReplaceIf(key, newValue, replaceCondition));
	}

	public bool TryReplace(TKey key, Func<TValue, TValue> replacementValue) {
		return FireCollectionChangedIf(dict.TryReplace(key, replacementValue));
	}

	public bool TryReplaceIf(TKey key, Func<TValue, TValue> replacementValue, Predicate<TValue> replaceCondition) {
		return FireCollectionChangedIf(dict.TryReplaceIf(key, replacementValue, replaceCondition));
	}

	public bool ReplaceAll(Func<TValue, TValue> replacementValue) {
		return FireCollectionChangedIf(dict.ReplaceAll(replacementValue));
	}

	public bool ReplaceAllIf(Func<TValue, TValue> replacementValue, Predicate<TValue> replaceCondition) {
		return FireCollectionChangedIf(dict.ReplaceAllIf(replacementValue, replaceCondition));
	}

	public bool Remove(TKey key) {
		return FireCollectionChangedIf(dict.Remove(key));
	}

	public bool RemoveIf(TKey key, Predicate<TValue> removeCondition) {
		return FireCollectionChangedIf(dict.RemoveIf(key, removeCondition));
	}

	public bool RemoveAll(Predicate<KeyValuePair<TKey, TValue>> removeCondition) {
		return FireCollectionChangedIf(dict.RemoveAll(removeCondition));
	}

	public ImmutableDictionary<TKey, TValue> ToImmutable() {
		return dict.ToImmutable();
	}

	public ImmutableDictionary<TKey, TNewValue> ToImmutable<TNewValue>(Func<TValue, TNewValue> valueSelector) {
		return dict.ToImmutable(valueSelector);
	}
}
