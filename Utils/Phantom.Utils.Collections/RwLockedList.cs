namespace Phantom.Utils.Collections; 

public sealed class RwLockedList<TItem> {
	private readonly List<TItem> list;
	private readonly ReaderWriterLockSlim rwLock;

	public RwLockedList(LockRecursionPolicy recursionPolicy) {
		this.list = new List<TItem>();
		this.rwLock = new ReaderWriterLockSlim(recursionPolicy);
	}

	public void Add(TItem item) {
		rwLock.EnterWriteLock();
		list.Add(item);
		rwLock.ExitWriteLock();
	}

	public void ForEach(Action<TItem> action) {
		rwLock.EnterReadLock();
		try {
			foreach (var item in list) {
				action(item);
			}
		} finally {
			rwLock.ExitReadLock();
		}
	}

	public void ForEachWith<TUser>(TUser userObject, Action<TItem, TUser> action) {
		rwLock.EnterReadLock();
		try {
			foreach (var item in list) {
				action(item, userObject);
			}
		} finally {
			rwLock.ExitReadLock();
		}
	}
}
