namespace Phantom.Utils.Events;

public sealed class EventPublisher<T> {
	public EventSubscribers<T> Subs { get; } = new ();

	public void Publish(T eventData) {
		Subs.Publish(eventData);
	}
}
