namespace Phantom.Utils.Events;

public class EventSubscribers<T> {
	private readonly Dictionary<object, Action<T>> subscribers = new (1);
	private readonly ReaderWriterLockSlim subscribersLock = new (LockRecursionPolicy.NoRecursion);

	public virtual void Subscribe(object owner, Action<T> subscriber) {
		subscribersLock.EnterWriteLock();
		subscribers[owner] = subscriber;
		subscribersLock.ExitWriteLock();
	}

	public virtual void Unsubscribe(object owner) {
		subscribersLock.EnterWriteLock();
		subscribers.Remove(owner);
		subscribersLock.ExitWriteLock();
	}

	internal void Publish(T eventData) {
		subscribersLock.EnterReadLock();
		var subs = subscribers.Values.ToArray(); // TODO optimize
		subscribersLock.ExitReadLock();
		
		foreach (var subscriber in subs) {
			subscriber(eventData);
		}
	}
}
