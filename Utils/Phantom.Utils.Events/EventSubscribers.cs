using Phantom.Utils.Collections;

namespace Phantom.Utils.Events;

public class EventSubscribers<T> {
	private readonly RwLockedDictionary<object, Action<T>> subscribers = new (1, LockRecursionPolicy.NoRecursion);

	public virtual void Subscribe(object owner, Action<T> subscriber) {
		subscribers[owner] = subscriber;
	}

	public virtual void Unsubscribe(object owner) {
		subscribers.Remove(owner);
	}

	internal void Publish(T eventData) {
		foreach (var subscriber in subscribers.ValuesCopy) { // TODO optimize
			subscriber(eventData);
		}
	}
}
