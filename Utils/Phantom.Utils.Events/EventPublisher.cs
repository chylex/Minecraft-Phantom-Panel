using Serilog;

namespace Phantom.Utils.Events;

public sealed class EventPublisher<T> {
	public EventSubscribers<T> Subs { get; }

	public EventPublisher(ILogger logger) {
		Subs = new EventSubscribers<T>(logger);
	}

	public void Publish(T eventData) {
		Subs.Publish(eventData);
	}
}
