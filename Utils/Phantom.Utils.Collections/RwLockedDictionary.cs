using System.Collections.Immutable;

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

	public void Remove(TKey key) {
		rwLock.EnterWriteLock();
		try {
			dict.Remove(key);
		} finally {
			rwLock.ExitWriteLock();
		}
	}

	public bool TryGetValue(TKey key, out TValue? value) {
		rwLock.EnterReadLock();
		try {
			return dict.TryGetValue(key, out value);
		} finally {
			rwLock.ExitReadLock();
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

	public bool TryRemove(TKey key, Predicate<TValue> removeCondition) {
		rwLock.EnterWriteLock();
		try {
			return dict.TryGetValue(key, out var oldValue) && removeCondition(oldValue) && dict.Remove(key);
		} finally {
			rwLock.ExitWriteLock();
		}
	}
}
