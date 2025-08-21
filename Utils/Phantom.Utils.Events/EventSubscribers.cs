using Phantom.Utils.Collections;
using Serilog;

namespace Phantom.Utils.Events;

public class EventSubscribers<T> {
	private readonly RwLockedDictionary<object, Action<T>> subscribers = new (1, LockRecursionPolicy.NoRecursion);
	private readonly ILogger logger;
	
	internal EventSubscribers(ILogger logger) {
		this.logger = logger;
	}
	
	public virtual void Subscribe(object owner, Action<T> subscriber) {
		subscribers[owner] = subscriber;
	}
	
	public virtual void Unsubscribe(object owner) {
		if (!subscribers.Remove(owner)) {
			logger.Warning("Tried unsubscribing an object that was not subscribed: {Owner}", owner);
		}
	}
	
	internal void Publish(T eventData) {
		subscribers.ForEachValue(subscriber => subscriber(eventData));
	}
}
