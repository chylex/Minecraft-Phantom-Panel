using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Phantom.Utils.Collections;

public sealed class RwLockedDictionary<TKey, TValue> where TKey : notnull {
	private readonly Dictionary<TKey, TValue> dict;
	private readonly ReaderWriterLockSlim rwLock;

	public RwLockedDictionary(LockRecursionPolicy recursionPolicy) {
		this.dict = new Dictionary<TKey, TValue>();
		this.rwLock = new ReaderWriterLockSlim(recursionPolicy);
	}

	public RwLockedDictionary(int capacity, LockRecursionPolicy recursionPolicy) {
		this.dict = new Dictionary<TKey, TValue>(capacity);
		this.rwLock = new ReaderWriterLockSlim(recursionPolicy);
	}

	public TValue this[TKey key] {
		get {
			rwLock.EnterReadLock();
			try {
				return dict[key];
			} finally {
				rwLock.ExitReadLock();
			}
		}

		set {
			rwLock.EnterWriteLock();
			try {
				dict[key] = value;
			} finally {
				rwLock.ExitWriteLock();
			}
		}
	}

	public ImmutableArray<TValue> ValuesCopy {
		get {
			rwLock.EnterReadLock();
			try {
				return dict.Values.ToImmutableArray();
			} finally {
				rwLock.ExitReadLock();
			}
		}
	}

	public void ForEachValue(Action<TValue> action) {
		rwLock.EnterReadLock();
		try {
			foreach (var value in dict.Values) {
				action(value);
			}
		} finally {
			rwLock.ExitReadLock();
		}
	}

	public bool AddOrReplace(TKey key, TValue newValue, [MaybeNullWhen(false)] out TValue oldValue) {
		rwLock.EnterWriteLock();
		try {
			bool hadValue = dict.TryGetValue(key, out oldValue);
			dict[key] = newValue;
			return hadValue;
		} finally {
			rwLock.ExitWriteLock();
		}
	}

	public bool Remove(TKey key) {
		rwLock.EnterWriteLock();
		try {
			return dict.Remove(key);
		} finally {
			rwLock.ExitWriteLock();
		}
	}

	public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value) {
		rwLock.EnterReadLock();
		try {
			return dict.TryGetValue(key, out value);
		} finally {
			rwLock.ExitReadLock();
		}
	}

	public bool TryAdd(TKey key, TValue newValue) {
		rwLock.EnterWriteLock();
		try {
			return dict.TryAdd(key, newValue);
		} finally {
			rwLock.ExitWriteLock();
		}
	}

	public bool TryAddOrReplace(TKey key, TValue newValue, Predicate<TValue> replaceCondition) {
		rwLock.EnterWriteLock();
		try {
			if (!dict.TryGetValue(key, out var oldValue) || replaceCondition(oldValue)) {
				dict[key] = newValue;
				return true;
			}
			else {
				return false;
			}
		} finally {
			rwLock.ExitWriteLock();
		}
	}

	public bool TryReplace(TKey key, Func<TValue, TValue> replacementValue) {
		return TryReplaceIf(key, replacementValue, static _ => true);
	}

	public bool TryReplaceIf(TKey key, Func<TValue, TValue> replacementValue, Predicate<TValue> replaceCondition) {
		rwLock.EnterWriteLock();
		try {
			if (dict.TryGetValue(key, out var oldValue) && replaceCondition(oldValue)) {
				dict[key] = replacementValue(oldValue);
				return true;
			}
			else {
				return false;
			}
		} finally {
			rwLock.ExitWriteLock();
		}
	}

	public bool TryReplaceAll(Func<TValue, TValue> replacementValue) {
		return TryReplaceAllIf(replacementValue, static _ => true);
	}

	public bool TryReplaceAllIf(Func<TValue, TValue> replacementValue, Predicate<TValue> replaceCondition) {
		rwLock.EnterWriteLock();
		try {
			bool hasChanged = false;
			
			foreach (var (key, oldValue) in dict) {
				if (replaceCondition(oldValue)) {
					dict[key] = replacementValue(oldValue);
					hasChanged = true;
				}
			}
			
			return hasChanged;
		} finally {
			rwLock.ExitWriteLock();
		}
	}

	public bool TryRemove(TKey key) {
		rwLock.EnterWriteLock();
		try {
			return dict.Remove(key);
		} finally {
			rwLock.ExitWriteLock();
		}
	}

	public bool TryRemoveIf(TKey key, Predicate<TValue> removeCondition) {
		rwLock.EnterWriteLock();
		try {
			return dict.TryGetValue(key, out var oldValue) && removeCondition(oldValue) && dict.Remove(key);
		} finally {
			rwLock.ExitWriteLock();
		}
	}

	public ImmutableDictionary<TKey, TValue> ToImmutable() {
		rwLock.EnterReadLock();
		try {
			return dict.ToImmutableDictionary();
		} finally {
			rwLock.ExitReadLock();
		}
	}
}
