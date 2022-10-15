using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Phantom.Utils.Collections;

[SuppressMessage("ReSharper", "MemberCanBeInternal")]
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

	public bool AddOrReplaceIf(TKey key, TValue newValue, Predicate<TValue> replaceCondition) {
		rwLock.EnterUpgradeableReadLock();
		try {
			if (dict.TryGetValue(key, out var oldValue) && !replaceCondition(oldValue)) {
				return false;
			}

			rwLock.EnterWriteLock();
			try {
				dict[key] = newValue;
				return true;
			} finally {
				rwLock.ExitWriteLock();
			}
		} finally {
			rwLock.ExitUpgradeableReadLock();
		}
	}

	public bool TryReplace(TKey key, Func<TValue, TValue> replacementValue) {
		return TryReplaceIf(key, replacementValue, static _ => true);
	}

	public bool TryReplaceIf(TKey key, Func<TValue, TValue> replacementValue, Predicate<TValue> replaceCondition) {
		rwLock.EnterUpgradeableReadLock();
		try {
			if (!dict.TryGetValue(key, out var oldValue) || !replaceCondition(oldValue)) {
				return false;
			}

			rwLock.EnterWriteLock();
			try {
				dict[key] = replacementValue(oldValue);
				return true;
			} finally {
				rwLock.ExitWriteLock();
			}
		} finally {
			rwLock.ExitUpgradeableReadLock();
		}
	}

	public bool ReplaceAll(Func<TValue, TValue> replacementValue) {
		rwLock.EnterWriteLock();
		try {
			foreach (var (key, oldValue) in dict) {
				dict[key] = replacementValue(oldValue);
			}

			return dict.Count > 0;
		} finally {
			rwLock.ExitWriteLock();
		}
	}

	public bool ReplaceAllIf(Func<TValue, TValue> replacementValue, Predicate<TValue> replaceCondition) {
		rwLock.EnterUpgradeableReadLock();
		try {
			bool hasChanged = false;

			try {
				foreach (var (key, oldValue) in dict) {
					if (replaceCondition(oldValue)) {
						if (!hasChanged) {
							rwLock.EnterWriteLock();
						}

						hasChanged = true;
						dict[key] = replacementValue(oldValue);
					}
				}
			} finally {
				if (hasChanged) {
					rwLock.ExitWriteLock();
				}
			}

			return hasChanged;
		} finally {
			rwLock.ExitUpgradeableReadLock();
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

	public bool RemoveIf(TKey key, Predicate<TValue> removeCondition) {
		rwLock.EnterUpgradeableReadLock();
		try {
			if (!dict.TryGetValue(key, out var oldValue) || !removeCondition(oldValue)) {
				return false;
			}

			rwLock.EnterWriteLock();
			try {
				return dict.Remove(key);
			} finally {
				rwLock.ExitWriteLock();
			}
		} finally {
			rwLock.ExitUpgradeableReadLock();
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
